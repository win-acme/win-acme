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
    internal partial class Program
    {
        private const string _authorizationValid = "valid";
        private const string _authorizationPending = "pending";
        private const string _authorizationInvalid = "invalid";

        private static RenewResult Renew(ScheduledRenewal renewal, RunLevel runLevel)
        {
            using (var scope = AutofacBuilder.Configuration(_container, renewal, runLevel))
            {
                return Renew(scope, renewal, runLevel);
            }
        }

        private static RenewResult Renew(ILifetimeScope renewalScope, ScheduledRenewal renewal, RunLevel runLevel)
        {
            using (var executionScope = AutofacBuilder.Execution(renewalScope, renewal, runLevel))
            {
                var targetPlugin = executionScope.Resolve<ITargetPlugin>();
                var originalBinding = renewal.Target;
                renewal.Target = targetPlugin.Refresh(renewal.Target);
                if (renewal.Target == null)
                {
                    renewal.Target = originalBinding;
                    return new RenewResult("Renewal target not found");
                }
                var split = targetPlugin.Split(renewal.Target);
                renewal.Target.AlternativeNames = split.SelectMany(s => s.AlternativeNames).ToList();
                var identifiers = split.SelectMany(t => t.GetHosts(false)).Distinct();
                var client = executionScope.Resolve<AcmeClient>();
                var order = client.CreateOrder(identifiers);
                var authorizations = new List<Authorization>();
                foreach (var authUrl in order.Payload.Authorizations)
                {
                    authorizations.Add(client.GetAuthorizationDetails(authUrl));
                }
                foreach (var target in split)
                {
                    foreach (var identifier in target.GetHosts(false))
                    {
                        var authorization = authorizations.FirstOrDefault(a => a.Identifier.Value == identifier);
                        var challenge = Authorize(executionScope, order, renewal.ValidationPluginOptions, target, authorization);
                        if (challenge.Status != _authorizationValid)
                        {
                            return OnRenewFail(challenge);
                        }
                    }
                }
                return OnRenewSuccess(executionScope, renewal, order);
            }
        }

        /// <summary>
        /// Steps to take on authorization failed
        /// </summary>
        /// <param name="auth"></param>
        /// <returns></returns>
        public static RenewResult OnRenewFail(Challenge challenge)
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
        private static RenewResult OnRenewSuccess(ILifetimeScope renewalScope, ScheduledRenewal renewal, OrderDetails order)
        {
            RenewResult result = null;
            try
            {
                var certificateService = renewalScope.Resolve<CertificateService>();
                var storePlugin = renewalScope.Resolve<IStorePlugin>();
                var oldCertificate = renewal.Certificate(storePlugin);
                var newCertificate = certificateService.RequestCertificate(renewal.Target, order);

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
                if (_options.Test &&
                    renewal.New &&
                    !_input.PromptYesNo($"[--test] Do you want to install the certificate?"))
                    return result;

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
                    var installFactories = renewalScope.Resolve<List<IInstallationPluginFactory>>();
                    var steps = installFactories.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var installFactory = installFactories[i];
                        if (!(installFactory is INull))
                        {
                            var installInstance = (IInstallationPlugin)renewalScope.Resolve(installFactory.Instance);
                            if (steps > 1)
                            {
                                _log.Information("Installation step {n}/{m}: {name}...", i + 1, steps, installFactory.Name);
                            }
                            else
                            {
                                _log.Information("Installing with {name}...", installFactory.Name);
                            }
                            installInstance.Install(newCertificate, oldCertificate);
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
                    (!_options.Test ||
                    _input.PromptYesNo($"[--test] Do you want to automatically renew this certificate?")))
                {
                    var taskScheduler = renewalScope.Resolve<TaskSchedulerService>();
                    taskScheduler.EnsureTaskScheduler();
                }

                return result;
            }
            catch (Exception ex)
            {
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
        private static Challenge Authorize(ILifetimeScope renewalScope, OrderDetails order, ValidationPluginOptions options, Target target, Authorization authorization)
        {
            var invalid = new Challenge { Status = _authorizationInvalid };
            var valid = new Challenge { Status = _authorizationValid };
            var client = renewalScope.Resolve<AcmeClient>();
            var identifier = authorization.Identifier.Value;
            try
            {
                _log.Information("Authorize identifier: {identifier}", identifier);
                if (authorization.Status == _authorizationValid && !_options.Test)
                {
                    _log.Information("Cached authorization result: {Status}", authorization.Status);
                    return valid;
                }
                else
                {
                    using (var identifierScope = AutofacBuilder.Validation(renewalScope, options, target, identifier))
                    {
                        IValidationPlugin validationPlugin = null;
                        try
                        {
                            validationPlugin = identifierScope.Resolve<IValidationPlugin>();
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

                        if (challenge.Status == _authorizationValid && !_options.Test)
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
                _log.Error("Error authorizing {target}", target);
                HandleException(ex);
                return invalid;
            }
        }
    }
}