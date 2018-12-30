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
        private const string _authorizationValid = "valid";
        private const string _authorizationPending = "pending";
        private const string _authorizationInvalid = "invalid";

        private RenewResult Renew(Renewal renewal, RunLevel runLevel)
        {
            using (var ts = _scopeBuilder.Target(_container, renewal, runLevel))
            using (var es = _scopeBuilder.Execution(ts, renewal, runLevel))
            {
                var targetPlugin = es.Resolve<ITargetPlugin>();
                var client = es.Resolve<AcmeClient>();
                var target = targetPlugin.Generate();
                if (target == null)
                {
                    throw new Exception($"Target plugin did not generate a target"); 
                }
                if (!target.IsValid(_log))
                {
                    throw new Exception($"Target plugin generated an invalid target");
                }
                var identifiers = target.GetHosts(false);
                var order = client.CreateOrder(identifiers);
                var authorizations = new List<Authorization>();
                foreach (var authUrl in order.Payload.Authorizations)
                {
                    authorizations.Add(client.GetAuthorizationDetails(authUrl));
                }
                foreach (var targetPart in target.Parts)
                {
                    foreach (var identifier in targetPart.GetHosts(false))
                    {
                        var authorization = authorizations.FirstOrDefault(a => a.Identifier.Value == identifier);
                        var challenge = Authorize(es, runLevel, order, renewal.ValidationPluginOptions, targetPart, authorization);
                        if (challenge.Status != _authorizationValid)
                        {
                            return OnRenewFail(challenge);
                        }
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
                var oldCertificate = renewal.Certificate(storePlugin);
                var newCertificate = certificateService.RequestCertificate(renewal, target, order);

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
                if (renewal.New && runLevel.HasFlag(RunLevel.Test) && !_input.PromptYesNo($"[--test] Do you want to install the certificate?"))
                {
                    return result;
                }

                try
                {
                    // Check if the newly requested certificate is already in the store, 
                    // which might be the case due to the cache mechanism built into the 
                    // RequestCertificate function
                    var storedCertificate = storePlugin.FindByThumbprint(newCertificate.Certificate.Thumbprint);
                    if (storedCertificate != null)
                    {
                        // Copy relevant properties
                        _log.Warning("Certificate with thumbprint {thumbprint} is already in the store", newCertificate.Certificate.Thumbprint);
                        newCertificate.Store = storedCertificate.Store;
                    }
                    else
                    {
                        // Save to store
                        storePlugin.Save(newCertificate);
                    }
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
                            installPlugin.Install(newCertificate, oldCertificate);
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

                // Add or update renewal
                if (renewal.New && 
                    !_options.NoTaskScheduler && 
                    (!runLevel.HasFlag(RunLevel.Test) || _input.PromptYesNo($"[--test] Do you want to automatically renew this certificate?")))
                {
                    var taskScheduler = renewalScope.Resolve<TaskSchedulerService>();
                    taskScheduler.EnsureTaskScheduler();
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
                            var details = client.GetChallengeDetails(authorization, challenge);
                            validationPlugin.PrepareChallenge(details);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error preparing for challenge answer");
                            return invalid;
                        }

                        _log.Debug("Submitting challenge answer");
                        challenge = client.SubmitChallengeAnswer(challenge);

                        // Have to loop to wait for server to stop being pending
                        var tries = 0;
                        var maxTries = 4;
                        while (challenge.Status == _authorizationPending)
                        {
                            _log.Debug("Refreshing authorization");
                            Thread.Sleep(2000); // this has to be here to give ACME server a chance to think
                            challenge = client.DecodeChallenge(challenge.Url);
                            tries += 1;
                            if (tries > maxTries)
                            {
                                _log.Error("Authorization timed out");
                                return invalid;
                            }
                        }

                        if (challenge.Status != _authorizationValid)
                        {
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