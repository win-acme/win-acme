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
using acme = ACMESharp.Protocol.Resources;

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

        /// <summary>
        /// Common objects used throughout the renewal process
        /// </summary>
        private class ExecutionContext
        {
            public ILifetimeScope Scope { get; private set; }
            public Order Order { get; private set; }
            public RunLevel RunLevel { get; private set; }
            public RenewResult Result { get; private set; }
            public Target Target => Order.Target;
            public Renewal Renewal => Order.Renewal;

            public ExecutionContext(ILifetimeScope scope, Order order, RunLevel runLevel, RenewResult result)
            {
                Scope = scope;
                Order = order;
                RunLevel = runLevel;
                Result = result;
            }
        }

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

        /// <summary>
        /// Determine if the renewal should be executes
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public async Task<RenewResult> HandleRenewal(Renewal renewal, RunLevel runLevel)
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

            // Create one or more orders based on the target
            var orderPlugin = es.Resolve<IOrderPlugin>();
            var orders = orderPlugin.Split(renewal, target);
            _log.Verbose("Target convert into {n} order(s)", orders.Count());

            // Check if renewal is needed
            if (!runLevel.HasFlag(RunLevel.ForceRenew) && !renewal.Updated)
            {
                _log.Verbose("Checking {renewal}", renewal.LastFriendlyName);
                if (!renewal.IsDue())
                {
                    var cs = es.Resolve<ICertificateService>();
                    var abort = true;
                    foreach (var order in orders)
                    {
                        var cache = cs.CachedInfo(order);
                        if (cache == null && !renewal.New)
                        {
                            _log.Information(LogType.All, "Renewal for {renewal} running prematurely due to detected target change", renewal.LastFriendlyName);
                            abort = false;
                            break;
                        }
                    }
                    if (abort)
                    {
                        _log.Information("Renewal for {renewal} is due after {date}", renewal.LastFriendlyName, renewal.GetDueDate());
                        return new RenewResult() { Abort = true };
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

            // If at this point we haven't retured already with an error/abort
            // actually execute the renewal
            return await ExecuteRenewal(es, orders.ToList(), runLevel);
        }

        /// <summary>
        /// Run the renewal 
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="orders"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<RenewResult> ExecuteRenewal(ILifetimeScope execute, List<Order> orders, RunLevel runLevel)
        {
            var result = new RenewResult();
            foreach (var order in orders)
            {
                _log.Verbose("Handle order {n}/{m} ({friendly})", 
                    orders.IndexOf(order) + 1,
                    orders.Count, 
                    order.FriendlyNamePart ?? "main");

                // Create the order details
                var orderManager = execute.Resolve<OrderManager>();
                order.Details = await orderManager.GetOrCreate(order, runLevel);

                // Create the execution context
                var context = new ExecutionContext(execute, order, runLevel, result);

                // Authorize the order (validation)
                await AuthorizeOrder(context);
                if (context.Result.Success)
                {
                    // Execute final steps (CSR, store, install)
                    await ExecuteOrder(context);
                }
            }
            return result;
        }

        /// <summary>
        /// Answer all the challenges in the order
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="order"></param>
        /// <param name="result"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task AuthorizeOrder(ExecutionContext context)
        {
            // Sanity check
            if (context.Order.Details == null)
            {
                context.Result.AddErrorMessage($"Unable to create order");
                return;
            }

            // Answer the challenges
            var client = context.Scope.Resolve<AcmeClient>();
            var authorizations = context.Order.Details.Payload.Authorizations.ToList();
            foreach (var authorizationUri in authorizations)
            {
                _log.Verbose("Handle authorization {n}/{m}",
                    authorizations.IndexOf(authorizationUri) + 1,
                    authorizations.Count);

                // Get authorization challenge details from server
                var authorization = await client.GetAuthorizationDetails(authorizationUri);

                // Find a targetPart that matches the challenge
                var targetPart = context.Target.Parts.
                    FirstOrDefault(tp => tp.GetHosts(false).
                    Any(h => authorization.Identifier.Value == h.Replace("*.", "")));
                if (targetPart == null)
                {
                    context.Result.AddErrorMessage("Unable to match challenge to target");
                    return;
                }

                // Run the validation plugin
                await HandleChallenge(context, targetPart, authorization);
            }
        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="partialTarget"></param>
        private async Task ExecuteOrder(ExecutionContext context)
        {
            try
            {
                var certificateService = context.Scope.Resolve<ICertificateService>();
                var csrPlugin = context.Target.CsrBytes == null ? 
                    context.Scope.Resolve<ICsrPlugin>() : 
                    null;
                if (csrPlugin != null)
                {
                    var (disabled, disabledReason) = csrPlugin.Disabled;
                    if (disabled)
                    {
                        context.Result.AddErrorMessage($"CSR plugin is not available. {disabledReason}");
                        return;
                    }
                }
                var oldCertificate = certificateService.CachedInfo(context.Order);
                var newCertificate = await certificateService.RequestCertificate(csrPlugin, context.RunLevel, context.Order);

                // Test if a new certificate has been generated 
                if (newCertificate == null)
                {
                    context.Result.AddErrorMessage("No certificate generated");
                    return;
                }
                else
                {
                    context.Result.AddThumbprint(newCertificate.Certificate.Thumbprint);
                }

                // Early escape for testing validation only
                if (context.Renewal.New &&
                    context.RunLevel.HasFlag(RunLevel.Test) &&
                    !await _input.PromptYesNo($"[--test] Do you want to install the certificate?", true))
                {
                    context.Result.Abort = true;
                    return;
                }

                // Run store plugin(s)
                var storePluginOptions = new List<StorePluginOptions>();
                var storePlugins = new List<IStorePlugin>();
                try
                {
                    var steps = context.Renewal.StorePluginOptions.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var storeOptions = context.Renewal.StorePluginOptions[i];
                        var storePlugin = (IStorePlugin)context.Scope.Resolve(storeOptions.Instance);
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
                                context.Result.AddErrorMessage($"Store plugin is not available. {disabledReason}");
                                return;
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
                    context.Result.AddErrorMessage($"Store failed: {reason}");
                    return;
                }

                // Run installation plugin(s)
                try
                {
                    var steps = context.Renewal.InstallationPluginOptions.Count();
                    for (var i = 0; i < steps; i++)
                    {
                        var installOptions = context.Renewal.InstallationPluginOptions[i];
                        var installPlugin = (IInstallationPlugin)context.Scope.Resolve(
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
                                context.Result.AddErrorMessage($"Installation plugin is not available. {disabledReason}");
                                return;
                            }
                            await installPlugin.Install(storePlugins, newCertificate, oldCertificate);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var reason = _exceptionHandler.HandleException(ex, "Unable to install certificate");
                    context.Result.AddErrorMessage($"Install failed: {reason}");
                    return;
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
                            // not a show-stopper, consider the renewal a success
                            context.Result.AddErrorMessage($"Delete failed: {ex.Message}", false);
                        }
                    }
                }

                if ((context.Renewal.New || context.Renewal.Updated) && !_args.NoTaskScheduler)
                {
                    if (context.RunLevel.HasFlag(RunLevel.Test) &&
                        !await _input.PromptYesNo($"[--test] Do you want to automatically renew this certificate?", true))
                    {
                        // Early out for test runs              
                        context.Result.Abort = true;
                        return;
                    }
                    else
                    {
                        // Make sure the Task Scheduler is configured
                        await context.Scope.Resolve<TaskSchedulerService>().EnsureTaskScheduler(context.RunLevel, false);
                    }
                }
            }
            catch (Exception ex)
            {
                var message = _exceptionHandler.HandleException(ex);
                context.Result.AddErrorMessage(message);
            }
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task HandleChallenge(ExecutionContext context, TargetPart targetPart, acme.Authorization authorization)
        {
            var valid = false;
            var client = context.Scope.Resolve<AcmeClient>();
            var identifier = authorization.Identifier.Value;
            var options = context.Renewal.ValidationPluginOptions;
            IValidationPlugin? validationPlugin = null;
            using var validation = _scopeBuilder.Validation(context.Scope, options, targetPart, identifier);
            try
            {
                if (authorization.Status == AcmeClient.AuthorizationValid)
                {
                    if (!context.RunLevel.HasFlag(RunLevel.Test) &&
                        !context.RunLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        _log.Information("Cached authorization result for {identifier}: {Status}", identifier, authorization.Status);
                        return;
                    }
                    // Used to make --force validation errors non-fatal
                    valid = true;
                }

                _log.Information("Authorize identifier {identifier}", identifier); 
                _log.Verbose("Initial authorization status: {status}", authorization.Status);
                _log.Verbose("Challenge types available: {challenges}", authorization.Challenges.Select(x => x.Type ?? "[Unknown]"));
                var challenge = authorization.Challenges.FirstOrDefault(c => string.Equals(c.Type, options.ChallengeType, StringComparison.CurrentCultureIgnoreCase));
                if (challenge == null)
                {
                    if (valid) 
                    {
                        var usedType = authorization.Challenges.
                            Where(x => x.Status == AcmeClient.ChallengeValid).
                            FirstOrDefault();
                        _log.Warning("Expected challenge type {type} not available for {identifier}, already validated using {valided}.",
                            options.ChallengeType,
                            authorization.Identifier.Value,
                            usedType?.Type ?? "[unknown]");
                        return;
                    } 
                    else
                    {
                        _log.Error("Expected challenge type {type} not available for {identifier}.",
                            options.ChallengeType,
                            authorization.Identifier.Value);
                        context.Result.AddErrorMessage("Expected challenge type not available", !valid);
                        return;
                    }
                } 
                else
                {
                    _log.Verbose("Initial challenge status: {status}", challenge.Status);
                    if (challenge.Status == AcmeClient.ChallengeValid)
                    {
                        // We actually should not get here because if one of the
                        // challenges is valid, the authorization itself should also 
                        // be valid.
                        if (!context.RunLevel.HasFlag(RunLevel.Test) &&
                            !context.RunLevel.HasFlag(RunLevel.IgnoreCache))
                        {
                            _log.Information("Cached challenge result: {Status}", authorization.Status);
                            return;
                        }
                    }
                }

                // We actually have to do validation now
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
                    _log.Error("Validation plugin not found or not created");
                    context.Result.AddErrorMessage("Validation plugin not found or not created", !valid);
                    return;
                }
                var (disabled, disabledReason) = validationPlugin.Disabled;
                if (disabled)
                {
                    _log.Error($"Validation plugin is not available. {disabledReason}");
                    context.Result.AddErrorMessage("Validation plugin is not available", !valid);
                    return;
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
                    context.Result.AddErrorMessage("Error preparing for challenge answer", !valid);
                    return;
                }

                _log.Debug("Submitting challenge answer");
                challenge = await client.AnswerChallenge(challenge);
                if (challenge.Status != AcmeClient.ChallengeValid)
                {
                    if (challenge.Error != null)
                    {
                        _log.Error(challenge.Error.ToString());
                    }
                    _log.Error("Authorization result: {Status}", challenge.Status);
                    context.Result.AddErrorMessage(challenge.Error?.ToString() ?? "Unspecified error", !valid);
                    return;
                }
                else
                {
                    _log.Information("Authorization result: {Status}", challenge.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error authorizing {renewal}", targetPart);
                var message = _exceptionHandler.HandleException(ex);
                context.Result.AddErrorMessage(message, !valid);
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
