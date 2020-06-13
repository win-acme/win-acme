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
            _input.CreateSpace();
            _log.Reset();
            using var ts = _scopeBuilder.Target(_container, renewal, runLevel);
            using var es = _scopeBuilder.Execution(ts, renewal, runLevel);
            // Generate the target
            var targetPlugin = es.Resolve<ITargetPlugin>();
            var (disabled, disabledReason) = targetPlugin.Disabled;
            if (disabled)
            {
                return new RenewResult($"Target plugin is not available. {disabledReason}");
            }
            var target = await targetPlugin.Generate();
            if (target is INull)
            {
                return new RenewResult($"Target plugin did not generate a target");
            }
            if (!target.IsValid(_log)) 
            { 
                return new RenewResult($"Target plugin generated an invalid target");
            }

            // Check if our validation plugin is (still) up to the task
            var validationPlugin = es.Resolve<IValidationPluginOptionsFactory>();
            if (!validationPlugin.CanValidate(target))
            {
                return new RenewResult($"Validation plugin is unable to validate the target. A wildcard host was introduced into a HTTP validated renewal.");
            }

            // Create one or more orders based on the target
            var orderPlugin = es.Resolve<IOrderPlugin>();
            var orders = orderPlugin.Split(renewal, target);
            if (orders == null || orders.Count() == 0)
            {
                return new RenewResult("Order plugin failed to create order(s)");
            }
            _log.Verbose("Targeted convert into {n} order(s)", orders.Count());

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
            var result = await ExecuteRenewal(es, orders.ToList(), runLevel);

            // Configure task scheduler
            if (result.Success && !result.Abort)
            {
                if ((renewal.New || renewal.Updated) && !_args.NoTaskScheduler)
                {
                    if (runLevel.HasFlag(RunLevel.Test) && !await _input.PromptYesNo($"[--test] Do you want to automatically renew with these settings?", true))
                    {
                        // Early out for test runs              
                        result.Abort = true;
                        return result;
                    }
                    else
                    {
                        // Make sure the Task Scheduler is configured
                        await es.Resolve<TaskSchedulerService>().EnsureTaskScheduler(runLevel, false);
                    }
                }
            }
            return result;
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
                _log.Verbose("Handle order {n}/{m}: {friendly}", 
                    orders.IndexOf(order) + 1,
                    orders.Count,
                    order.FriendlyNamePart ?? "Main");

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
                var options = context.Renewal.ValidationPluginOptions;
                using var validation = _scopeBuilder.Validation(context.Scope, options);
                var validationContext = new ValidationContext(validation, authorization, targetPart, options.ChallengeType, options.Name);
                // Prepare answer
                await PrepareChallengeAnswer(validationContext, context.RunLevel);
                if (context.Result.Success)
                {
                    // Submit for validation
                    await AnswerChallenge(validationContext);
                    TransferErrors(validationContext, context.Result, authorization.Identifier.Value);
                }
                if (validationContext.Challenge != null)
                {
                    // Cleanup
                    await CleanValidation(validationContext);
                    TransferErrors(validationContext, context.Result, authorization.Identifier.Value);
                }
                if (!context.Result.Success)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Move errors from a validation context up to the renewal result
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="prefix"></param>
        private void TransferErrors(ValidationContext from, RenewResult to, string prefix) => 
            from.ErrorMessages.ForEach(e => to.AddErrorMessage($"[{prefix}] {e}", from.Success == false));

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
                        var storePlugin = (IStorePlugin)context.Scope.Resolve(storeOptions.Instance,
                            new TypedParameter(storeOptions.GetType(), storeOptions));
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
                            await installPlugin.Install(context.Target, storePlugins, newCertificate, oldCertificate);
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
        private async Task PrepareChallengeAnswer(ValidationContext context, RunLevel runLevel)
        {
            var client = context.Scope.Resolve<AcmeClient>();
            try
            {
                if (context.Authorization.Status == AcmeClient.AuthorizationValid)
                {
                    _log.Information("[{identifier}] Cached authorization result: {Status}", context.Identifier, context.Authorization.Status);
                    if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        return;
                    }
                    // Used to make --force or --test re-validation errors non-fatal
                    _log.Information("[{identifier}] Handling challenge anyway because --test and/or --force is active");
                    context.Success = true;
                }

                _log.Information("[{identifier}] Authorizing...", context.Identifier);
                _log.Verbose("[{identifier}] Initial authorization status: {status}", context.Identifier, context.Authorization.Status);
                _log.Verbose("[{identifier}] Challenge types available: {challenges}", context.Identifier, context.Authorization.Challenges.Select(x => x.Type ?? "[Unknown]"));
                var challenge = context.Authorization.Challenges.FirstOrDefault(c => string.Equals(c.Type, context.ChallengeType, StringComparison.CurrentCultureIgnoreCase));
                if (challenge == null)
                {
                    if (context.Success == true)
                    {
                        var usedType = context.Authorization.Challenges.
                            Where(x => x.Status == AcmeClient.ChallengeValid).
                            FirstOrDefault();
                        _log.Warning("[{identifier}] Expected challenge type {type} not available, already validated using {valided}.",
                            context.Identifier,
                            context.ChallengeType,
                            usedType?.Type ?? "[unknown]");
                        return;
                    }
                    else
                    {
                        _log.Error("[{identifier}] Expected challenge type {type} not available.",
                            context.Identifier,
                            context.ChallengeType);
                        context.AddErrorMessage("Expected challenge type not available", context.Success == false);
                        return;
                    }
                }
                else
                {
                    _log.Verbose("[{identifier}] Initial challenge status: {status}", context.Identifier, challenge.Status);
                    if (challenge.Status == AcmeClient.ChallengeValid)
                    {
                        // We actually should not get here because if one of the
                        // challenges is valid, the authorization itself should also 
                        // be valid.
                        if (!runLevel.HasFlag(RunLevel.Test) && !runLevel.HasFlag(RunLevel.IgnoreCache))
                        {
                            _log.Information("[{identifier}] Cached challenge result: {Status}", context.Identifier, context.Authorization.Status);
                            return;
                        }
                    }
                }

                // We actually have to do validation now
                try
                {
                    context.ValidationPlugin = context.Scope.Resolve<IValidationPlugin>();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[{identifier}] Error resolving validation plugin", context.Identifier);
                }
                if (context.ValidationPlugin == null)
                {
                    _log.Error("[{identifier}] Validation plugin not found or not created", context.Identifier);
                    context.AddErrorMessage("Validation plugin not found or not created", context.Success == false);
                    return;
                }
                var (disabled, disabledReason) = context.ValidationPlugin.Disabled;
                if (disabled)
                {
                    _log.Error($"[{{identifier}}] Validation plugin is not available. {disabledReason}", context.Identifier);
                    context.AddErrorMessage("Validation plugin is not available", context.Success == false);
                    return;
                }
                _log.Information("[{identifier}] Authorizing using {challengeType} validation ({name})",
                    context.Identifier,
                    context.ChallengeType,
                    context.PluginName);
                try
                {
                    // Now that we're going to call into PrepareChallenge, we will assume 
                    // responsibility to also call CleanUp later, which is signalled by
                    // the Challenge propery being not null
                    context.ChallengeDetails = await client.DecodeChallengeValidation(context.Authorization, challenge);
                    context.Challenge = challenge;
                    await context.ValidationPlugin.PrepareChallenge(context);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[{identifier}] Error preparing for challenge answer", context.Identifier);
                    context.AddErrorMessage("Error preparing for challenge answer", context.Success == false);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error preparing challenge answer", context.Identifier);
                var message = _exceptionHandler.HandleException(ex);
                context.AddErrorMessage(message, context.Success == false);
            }
        }

        /// <summary>
        /// Make sure we have authorization for every host in target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task AnswerChallenge(ValidationContext validationContext)
        {
            if (validationContext.Challenge == null)
            {
                throw new InvalidOperationException();
            }
            try
            {
                _log.Debug("[{identifier}] Submitting challenge answer", validationContext.Identifier);
                var client = validationContext.Scope.Resolve<AcmeClient>();
                var updatedChallenge = await client.AnswerChallenge(validationContext.Challenge);
                validationContext.Challenge = updatedChallenge;
                if (updatedChallenge.Status != AcmeClient.ChallengeValid)
                {
                    if (updatedChallenge.Error != null)
                    {
                        _log.Error(updatedChallenge.Error.ToString());
                    }
                    _log.Error("[{identifier}] Authorization result: {Status}", validationContext.Identifier, updatedChallenge.Status);
                    validationContext.AddErrorMessage(updatedChallenge.Error?.ToString() ?? "Unspecified error", validationContext.Success == false);
                    return;
                }
                else
                {
                    _log.Information("[{identifier}] Authorization result: {Status}", validationContext.Identifier, updatedChallenge.Status);
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[{identifier}] Error submitting challenge answer", validationContext.Identifier);
                var message = _exceptionHandler.HandleException(ex);
                validationContext.AddErrorMessage(message, validationContext.Success == false);
            } 
        }

        /// <summary>
        /// Clean up after (succesful or unsuccesful) validation attempt
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private async Task CleanValidation(ValidationContext validationContext)
        {
            if (validationContext.Challenge == null || 
                validationContext.ValidationPlugin == null)
            {
                throw new InvalidOperationException();
            }
            try
            {
                _log.Verbose("[{identifier}] Starting post-validation cleanup", validationContext.Identifier);
               await validationContext.ValidationPlugin.CleanUp(validationContext);
                _log.Verbose("[{identifier}] Post-validation cleanup was succesful", validationContext.Identifier);
            }
            catch (Exception ex)
            {
                _log.Warning("[{identifier}] An error occured during post-validation cleanup: {ex}", ex.Message, validationContext.Identifier);
            }
        }
    }
}
