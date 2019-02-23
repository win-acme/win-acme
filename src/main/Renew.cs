using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Autofac;
using PKISharp.WACS.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PKISharp.WACS
{
    /// <summary>
    /// This part of the code handles the actual creation/renewal of ACME certificates
    /// </summary>
    internal partial class Wacs
    {
        private const string _orderReady = "ready";
        private const string _orderPending = "pending";

        private const string _authorizationValid = "valid";
        private const string _authorizationPending = "pending";
        private const string _authorizationInvalid = "invalid";

        private RenewResult Renew(Renewal renewal, RunLevel runLevel)
        {
            using (var ts = _scopeBuilder.Target(_container, renewal, runLevel))
            using (var es = _scopeBuilder.Execution(ts, renewal, runLevel))
            {
                // Generate the target
                var targetPlugin = es.Resolve<ITargetPlugin>();
                var target = targetPlugin.Generate();
                if (target == null)
                {
                    throw new Exception($"Target plugin did not generate a target"); 
                }
                if (!target.IsValid(_log))
                {
                    throw new Exception($"Target plugin generated an invalid target");
                }
   
                // Check if our validation plugin is (still) up to the task
                var validationPlugin = es.Resolve<IValidationPluginOptionsFactory>();
                if (!validationPlugin.CanValidate(target))
                {
                    throw new Exception($"Validation plugin is unable to validate the target. A wildcard host was introduced into a HTTP validated renewal.");
                }

                // Check if renewal is needed
                if (!runLevel.HasFlag(RunLevel.ForceRenew) && !renewal.Updated)
                {
                    _log.Verbose("Checking {renewal}", renewal.LastFriendlyName);
                    if (renewal.Date >= DateTime.Now)
                    {
                        var cs = es.Resolve<CertificateService>();
                        var cache = cs.CachedInfo(renewal);
                        if (cache != null && cache.Match(target))
                        {
                            _log.Information(true, "Renewal for {renewal} is due after {date}", renewal.LastFriendlyName, renewal.Date.ToUserString());
                            return null;
                        }
                        else
                        {
                            _log.Information(true, "Renewal for {renewal} running prematurely due to detected target change", renewal.LastFriendlyName);
                        }
                    }
                    else if (!renewal.New)
                    {
                        _log.Information(true, "Renewing certificate for {renewal}", renewal.LastFriendlyName);
                    }
                }

                // Create the order
                var client = es.Resolve<AcmeClient>();
                var identifiers = target.GetHosts(false);
                var order = client.CreateOrder(identifiers);

                // Check if the order is valid
                if (order.Payload.Status != _orderReady && 
                    order.Payload.Status != _orderPending)
                {
                    return OnRenewFail(new Challenge() { Error = order.Payload.Error });
                }

                // Answer the challenges
                foreach (var authUrl in order.Payload.Authorizations)
                {
                    // Get authorization details
                    var authorization = client.GetAuthorizationDetails(authUrl);

                    // Find a targetPart that matches the challenge
                    var targetPart = target.Parts.
                        FirstOrDefault(tp => tp.GetHosts(false).
                        Any(h => authorization.Identifier.Value == h.Replace("*.", "")));
                    if (targetPart == null)
                    {
                        return OnRenewFail(new Challenge() {
                            Error = "Unable to match challenge to target"
                        });
                    }

                    // Run the validation plugin
                    var challenge = Authorize(es, runLevel, order, renewal.ValidationPluginOptions, targetPart, authorization);
                    if (challenge.Status != _authorizationValid)
                    {
                        return OnRenewFail(challenge);
                    }
                }
                return OnRenewSuccess(es, renewal, target, order, runLevel);
            }
        }

        /// <summary>
        /// Steps to take on authorization failed
        /// </summary>
        /// <param name="auth"></param>
        /// <returns></returns>
        public RenewResult OnRenewFail(Challenge challenge)
        {
            var errors = challenge?.Error;
            if (errors != null)
            {
                _log.Error("ACME server reported:");
                _log.Error("{@value}", errors);
            }
            return new RenewResult("Authorization failed");

        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="target"></param>
        private RenewResult OnRenewSuccess(ILifetimeScope renewalScope, Renewal renewal, Target target, OrderDetails order, RunLevel runLevel)
        {
            RenewResult result = null;
            try
            {
                var certificateService = renewalScope.Resolve<CertificateService>();
                var storePlugin = renewalScope.Resolve<IStorePlugin>();
                var csrPlugin = renewalScope.Resolve<ICsrPlugin>();
                var oldCertificate = certificateService.CachedInfo(renewal);
                var newCertificate = certificateService.RequestCertificate(csrPlugin, renewal, target, order);

                // Test if a new certificate has been generated 
                if (newCertificate == null)
                {
                    return new RenewResult("No certificate generated");
                }
                else
                {
                    result = new RenewResult(newCertificate);
                }

                // Early escape for testing validation only
                if (renewal.New && runLevel.HasFlag(RunLevel.Test) && !_input.PromptYesNo($"[--test] Do you want to install the certificate?", true))
                {
                    return new RenewResult("User aborted");
                }

                try
                {
                    storePlugin.Save(newCertificate);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to store certificate");
                    result.Success = false;
                    result.ErrorMessage = $"Store failed: {ex.Message}";
                    return result;
                }

                // Run installation plugin(s)
                try
                {
                    var steps = renewal.InstallationPluginOptions.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var installOptions = renewal.InstallationPluginOptions[i];
                        var installPlugin = (IInstallationPlugin)renewalScope.Resolve(installOptions.Instance);
                        if (!(installPlugin is INull))
                        {
                            if (steps > 1)
                            {
                                _log.Information("Installation step {n}/{m}: {name}...", i + 1, steps, installOptions.Name);
                            }
                            else
                            {
                                _log.Information("Installing with {name}...", installOptions.Name);
                            }
                            installPlugin.Install(storePlugin, newCertificate, oldCertificate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to install certificate");
                    result.Success = false;
                    result.ErrorMessage = $"Install failed: {ex.Message}";
                }

                // Delete the old certificate if not forbidden, found and not re-used
                if (!renewal.StorePluginOptions.KeepExisting &&
                    oldCertificate != null &&
                    newCertificate.Certificate.Thumbprint != oldCertificate.Certificate.Thumbprint)
                {
                    try
                    {
                        storePlugin.Delete(oldCertificate);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to delete previous certificate");
                        //result.Success = false; // not a show-stopper, consider the renewal a success
                        result.ErrorMessage = $"Delete failed: {ex.Message}";
                    }
                }

                if (renewal.New && !_args.NoTaskScheduler)
                {
                    if (runLevel.HasFlag(RunLevel.Test) && !_input.PromptYesNo($"[--test] Do you want to automatically renew this certificate?", true))
                    {
                        // Early out for test runs
                        return new RenewResult("User aborted");
                    }
                    else
                    {
                        // Make sure the Task Scheduler is configured
                        renewalScope.Resolve<TaskSchedulerService>().EnsureTaskScheduler();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex);

                // Result might still contain the Thumbprint of the certificate 
                // that was requested and (partially? installed, which might help
                // with debugging
                if (result == null)
                {
                    result = new RenewResult(ex.Message);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }
            }

            return result;
        }
        
        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Challenge Authorize(ILifetimeScope execute, RunLevel runLevel, OrderDetails order, ValidationPluginOptions options, TargetPart targetPart, Authorization authorization)
        {
            var invalid = new Challenge { Status = _authorizationInvalid };
            var valid = new Challenge { Status = _authorizationValid };
            var client = execute.Resolve<AcmeClient>();
            var identifier = authorization.Identifier.Value;
            try
            {
                _log.Information("Authorize identifier: {identifier}", identifier);
                if (authorization.Status == _authorizationValid && !runLevel.HasFlag(RunLevel.Test))
                {
                    _log.Information("Cached authorization result: {Status}", authorization.Status);
                    return valid;
                }
                else
                {
                    using (var validation = _scopeBuilder.Validation(execute, options, targetPart, identifier))
                    {
                        IValidationPlugin validationPlugin = null;
                        try
                        {
                            validationPlugin = validation.Resolve<IValidationPlugin>();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error resolving validation plugin");
                        }
                        if (validationPlugin == null)
                        {
                            _log.Error("Validation plugin not found or not created.");
                            return invalid;
                        }
                        var challenge = authorization.Challenges.FirstOrDefault(c => c.Type == options.ChallengeType);
                        if (challenge == null)
                        {
                            _log.Error("Expected challenge type {type} not available for {identifier}.",
                                options.ChallengeType,
                                authorization.Identifier.Value);
                            return invalid;
                        }

                        if (challenge.Status == _authorizationValid && !runLevel.HasFlag(RunLevel.Test))
                        {
                            _log.Information("{dnsIdentifier} already validated by {challengeType} validation ({name})",
                                 authorization.Identifier.Value,
                                 options.ChallengeType,
                                 options.Name);
                            return valid;
                        }

                        _log.Information("Authorizing {dnsIdentifier} using {challengeType} validation ({name})",
                            identifier,
                            options.ChallengeType,
                            options.Name);
                        try
                        {
                            var details = client.DecodeChallengeValidation(authorization, challenge);
                            validationPlugin.PrepareChallenge(details);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error preparing for challenge answer");
                            return invalid;
                        }

                        _log.Debug("Submitting challenge answer");
                        challenge = client.AnswerChallenge(challenge);

                        // Have to loop to wait for server to stop being pending
                        var tries = 0;
                        var maxTries = 4;
                        while (challenge.Status == _authorizationPending)
                        {
                            _log.Debug("Refreshing authorization");
                            Thread.Sleep(2000); // this has to be here to give ACME server a chance to think
                            challenge = client.GetChallengeDetails(challenge.Url);
                            tries += 1;
                            if (tries > maxTries)
                            {
                                _log.Error("Authorization timed out");
                                return invalid;
                            }
                        }

                        if (challenge.Status != _authorizationValid)
                        {
                            if (challenge.Error != null)
                            {
                                _log.Error(challenge.Error.ToString());
                            }
                            _log.Error("Authorization result: {Status}", challenge.Status);
                            return invalid;
                        }
                        else
                        {
                            _log.Information("Authorization result: {Status}", challenge.Status);
                            return valid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error authorizing {renewal}", targetPart);
                HandleException(ex);
                return invalid;
            }
        }
    }
}