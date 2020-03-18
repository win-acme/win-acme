using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Autofac;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    /// <summary>
    /// This part of the code handles the actual creation/renewal of ACME certificates
    /// </summary>
    internal class RenewalExecutor
    {
        private readonly MainArguments _args;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ILifetimeScope _container;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ExceptionHandler _exceptionHandler;

        public RenewalExecutor(
            MainArguments args, IAutofacBuilder scopeBuilder,
            ILogService log, IInputService input,
            ExceptionHandler exceptionHandler, IContainer container)
        {
            _args = args;
            _scopeBuilder = scopeBuilder;
            _log = log;
            _input = input;
            _exceptionHandler = exceptionHandler;
            _container = container;
        }

        public async Task<RenewResult?> Execute(Renewal renewal, RunLevel runLevel)
        {
            using var ts = _scopeBuilder.Target(_container, renewal, runLevel);
            using var es = _scopeBuilder.Execution(ts, renewal, runLevel);
            // Generate the target
            var targetPlugin = es.Resolve<ITargetPlugin>();
            var (disabled, disabledReason) = targetPlugin.Disabled;
            if (disabled)
            {
                throw new Exception($"Target plugin is not available. {disabledReason}");
            }
            var target = await targetPlugin.Generate();
            if (target is INull)
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
                if (!renewal.IsDue())
                {
                    var cs = es.Resolve<ICertificateService>();
                    var cache = cs.CachedInfo(renewal, target);
                    if (cache != null)
                    {
                        _log.Information("Renewal for {renewal} is due after {date}", renewal.LastFriendlyName, renewal.GetDueDate());
                        return null;
                    }
                    else if (!renewal.New)
                    {
                        _log.Information(LogType.All, "Renewal for {renewal} running prematurely due to detected target change", renewal.LastFriendlyName);
                    }
                }
                else if (!renewal.New)
                {
                    _log.Information(LogType.All, "Renewing certificate for {renewal}", renewal.LastFriendlyName);
                }
            }
            else if (runLevel.HasFlag(RunLevel.ForceRenew))
            {
                _log.Information(LogType.All, "Force renewing certificate for {renewal}", renewal.LastFriendlyName);
            }

            // Create the order
            var orderManager = es.Resolve<OrderManager>();
            var order = await orderManager.GetOrCreate(renewal, target);
            if (order == null)
            {
                return OnRenewFail(new Challenge() { Error = "Unable to create order" });
            }

            // Answer the challenges
            var client = es.Resolve<AcmeClient>();
            foreach (var authUrl in order.Payload.Authorizations)
            {
                // Get authorization details
                _log.Verbose("Handle authorization {n}/{m}", 
                    order.Payload.Authorizations.ToList().IndexOf(authUrl) + 1,
                    order.Payload.Authorizations.Length + 1);

                var authorization = await client.GetAuthorizationDetails(authUrl);

                // Find a targetPart that matches the challenge
                var targetPart = target.Parts.
                    FirstOrDefault(tp => tp.GetHosts(false).
                    Any(h => authorization.Identifier.Value == h.Replace("*.", "")));
                if (targetPart == null)
                {
                    return OnRenewFail(new Challenge()
                    {
                        Error = "Unable to match challenge to target"
                    });
                }

                // Run the validation plugin
                var challenge = await Authorize(es, runLevel, renewal.ValidationPluginOptions, targetPart, authorization);
                if (challenge.Status != AcmeClient.AuthorizationValid)
                {
                    return OnRenewFail(challenge);
                }
            }
            return await OnValidationSuccess(es, renewal, target, order, runLevel);
        }

        /// <summary>
        /// Steps to take on authorization failed
        /// </summary>
        /// <param name="auth"></param>
        /// <returns></returns>
        private RenewResult OnRenewFail(Challenge challenge)
        {
            var errors = challenge?.Error;
            if (errors != null)
            {
                return new RenewResult($"Authorization failed: {errors.ToString()}");
            } 
            else
            {
                return new RenewResult($"Authorization failed");
            }
        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="target"></param>
        private async Task<RenewResult> OnValidationSuccess(ILifetimeScope renewalScope, Renewal renewal, Target target, OrderDetails order, RunLevel runLevel)
        {
            RenewResult? result = null;
            try
            {
                var certificateService = renewalScope.Resolve<ICertificateService>();
                var csrPlugin = target.CsrBytes == null ? renewalScope.Resolve<ICsrPlugin>() : null;
                if (csrPlugin != null)
                {
                    var (disabled, disabledReason) = csrPlugin.Disabled;
                    if (disabled)
                    {
                        return new RenewResult($"CSR plugin is not available. {disabledReason}");
                    }
                }
                var oldCertificate = certificateService.CachedInfo(renewal);
                var newCertificate = await certificateService.RequestCertificate(csrPlugin, runLevel, renewal, target, order);

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
                if (renewal.New &&
                    runLevel.HasFlag(RunLevel.Test) &&
                    !await _input.PromptYesNo($"[--test] Do you want to install the certificate?", true))
                {
                    return new RenewResult("User aborted");
                }

                // Run store plugin(s)
                var storePluginOptions = new List<StorePluginOptions>();
                var storePlugins = new List<IStorePlugin>();
                try
                {
                    var steps = renewal.StorePluginOptions.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var storeOptions = renewal.StorePluginOptions[i];
                        var storePlugin = (IStorePlugin)renewalScope.Resolve(storeOptions.Instance);
                        if (!(storePlugin is INull))
                        {
                            if (steps > 1)
                            {
                                _log.Information("Store step {n}/{m}: {name}...", i + 1, steps, storeOptions.Name);
                            }
                            else
                            {
                                _log.Information("Store with {name}...", storeOptions.Name);
                            }
                            var (disabled, disabledReason) = storePlugin.Disabled;
                            if (disabled)
                            {
                                return new RenewResult($"Store plugin is not available. {disabledReason}");
                            }
                            await storePlugin.Save(newCertificate);
                            storePlugins.Add(storePlugin);
                            storePluginOptions.Add(storeOptions);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var reason = _exceptionHandler.HandleException(ex, "Unable to store certificate");
                    result.ErrorMessage = $"Store failed: {reason}";
                    result.Success = false;
                    return result;
                }

                // Run installation plugin(s)
                try
                {
                    var steps = renewal.InstallationPluginOptions.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var installOptions = renewal.InstallationPluginOptions[i];
                        var installPlugin = (IInstallationPlugin)renewalScope.Resolve(
                            installOptions.Instance,
                            new TypedParameter(installOptions.GetType(), installOptions));

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
                            var (disabled, disabledReason) = installPlugin.Disabled;
                            if (disabled)
                            {
                                return new RenewResult($"Installation plugin is not available. {disabledReason}");
                            }
                            await installPlugin.Install(storePlugins, newCertificate, oldCertificate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var reason = _exceptionHandler.HandleException(ex, "Unable to install certificate");
                    result.Success = false;
                    result.ErrorMessage = $"Install failed: {reason}";
                }

                // Delete the old certificate if not forbidden, found and not re-used
                for (var i = 0; i < storePluginOptions.Count; i++)
                {
                    if (!storePluginOptions[i].KeepExisting &&
                        oldCertificate != null &&
                        newCertificate.Certificate.Thumbprint != oldCertificate.Certificate.Thumbprint)
                    {
                        try
                        {
                            await storePlugins[i].Delete(oldCertificate);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Unable to delete previous certificate");
                            //result.Success = false; // not a show-stopper, consider the renewal a success
                            result.ErrorMessage = $"Delete failed: {ex.Message}";
                        }
                    }
                }

                if ((renewal.New || renewal.Updated) && !_args.NoTaskScheduler)
                {
                    if (runLevel.HasFlag(RunLevel.Test) &&
                        !await _input.PromptYesNo($"[--test] Do you want to automatically renew this certificate?", true))
                    {
                        // Early out for test runs
                        return new RenewResult("User aborted");
                    }
                    else
                    {
                        // Make sure the Task Scheduler is configured
                        await renewalScope.Resolve<TaskSchedulerService>().EnsureTaskScheduler(runLevel, false);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

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
        private async Task<Challenge> Authorize(
            ILifetimeScope execute, RunLevel runLevel,
            ValidationPluginOptions options, TargetPart targetPart,
            Authorization authorization)
        {
            var invalid = new Challenge { Status = AcmeClient.AuthorizationInvalid };
            var valid = new Challenge { Status = AcmeClient.AuthorizationValid };
            var client = execute.Resolve<AcmeClient>();
            var identifier = authorization.Identifier.Value;
            IValidationPlugin? validationPlugin = null;
            try
            {
                if (authorization.Status == AcmeClient.AuthorizationValid)
                {
                    if (!runLevel.HasFlag(RunLevel.Test) &&
                        !runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        _log.Information("Cached authorization result: {Status}", authorization.Status);
                        return valid;
                    }

                    if (runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        // Due to the IgnoreCache flag (--force switch) 
                        // we are going to attempt to re-authorize the 
                        // domain even though its already autorized. 
                        // On failure, we can still use the cached result. 
                        // This helps for migration scenarios.
                        invalid = valid;
                    }
                }

                _log.Information("Authorize identifier: {identifier}", identifier);
                _log.Verbose("Challenge types available: {challenges}", authorization.Challenges.Select(x => x.Type ?? "[Unknown]"));
                var challenge = authorization.Challenges.FirstOrDefault(c => string.Equals(c.Type, options.ChallengeType, StringComparison.CurrentCultureIgnoreCase));
                if (challenge == null)
                {
                    if (authorization.Status == AcmeClient.AuthorizationValid) 
                    {
                        var usedType = authorization.Challenges.
                            Where(x => x.Status == AcmeClient.AuthorizationValid).
                            FirstOrDefault();
                        _log.Warning("Expected challenge type {type} not available for {identifier}, already validated using {valided}.",
                            options.ChallengeType,
                            authorization.Identifier.Value,
                            usedType?.Type ?? "[unknown]");
                        return valid;
                    } 
                    else
                    {
                        _log.Error("Expected challenge type {type} not available for {identifier}.",
                            options.ChallengeType,
                            authorization.Identifier.Value);
                        invalid.Error = "Expected challenge type not available";
                        return invalid;
                    }
                }

                // We actually have to do validation now
                using var validation = _scopeBuilder.Validation(execute, options, targetPart, identifier);
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
                    invalid.Error = "Validation plugin not found or not created.";
                    return invalid;
                }
                var (disabled, disabledReason) = validationPlugin.Disabled;
                if (disabled)
                {
                    _log.Error($"Validation plugin is not available. {disabledReason}");
                    invalid.Error = "Validation plugin is not available.";
                    return invalid;
                }
                _log.Information("Authorizing {dnsIdentifier} using {challengeType} validation ({name})",
                    identifier,
                    options.ChallengeType,
                    options.Name);
                try
                {
                    var details = await client.DecodeChallengeValidation(authorization, challenge);
                    await validationPlugin.PrepareChallenge(details);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error preparing for challenge answer");
                    invalid.Error = "Error preparing for challenge answer";
                    return invalid;
                }

                _log.Debug("Submitting challenge answer");
                challenge = await client.AnswerChallenge(challenge);
                if (challenge.Status != AcmeClient.AuthorizationValid)
                {
                    if (challenge.Error != null)
                    {
                        _log.Error(challenge.Error.ToString());
                    }
                    _log.Error("Authorization result: {Status}", challenge.Status);
                    invalid.Error = challenge.Error;
                    return invalid;
                }
                else
                {
                    _log.Information("Authorization result: {Status}", challenge.Status);
                    return valid;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error authorizing {renewal}", targetPart);
                _exceptionHandler.HandleException(ex);
                invalid.Error = ex.Message;
                return invalid;
            } 
            finally
            {
                if (validationPlugin != null)
                {
                    try
                    {
                        _log.Verbose("Starting post-validation cleanup");
                        await validationPlugin.CleanUp();
                        _log.Verbose("Post-validation cleanup was succesful");
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("An error occured during post-validation cleanup: {ex}", ex.Message);
                    }
                }
            }
        }
    }
}
