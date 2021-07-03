using Autofac;
using Newtonsoft.Json.Schema;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Context;
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
        private readonly RenewalValidator _validator;

        public RenewalExecutor(
            MainArguments args, IAutofacBuilder scopeBuilder,
            ILogService log, IInputService input,
            RenewalValidator validator,
            ExceptionHandler exceptionHandler, IContainer container)
        {
            _validator = validator;
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
                return new RenewResult($"Source plugin is not available. {disabledReason}");
            }
            var target = await targetPlugin.Generate();
            if (target is INull)
            {
                return new RenewResult($"Source plugin did not generate source");
            }
            if (!target.IsValid(_log)) 
            { 
                return new RenewResult($"Source plugin generated invalid source");
            }

            // Check if our validation plugin is (still) up to the task
            var validationPlugin = es.Resolve<IValidationPluginOptionsFactory>();
            if (!validationPlugin.CanValidate(target))
            {
                return new RenewResult($"Validation plugin is unable to validate the source. A wildcard host was introduced into a HTTP validated renewal.");
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
                        await es.Resolve<TaskSchedulerService>().EnsureTaskScheduler(runLevel);
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
                await _validator.AuthorizeOrder(context, runLevel);
                if (context.Result.Success)
                {
                    // Execute final steps (CSR, store, install)
                    await ExecuteOrder(context);
                }
            }
            return result;
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
                            if (!await installPlugin.Install(context.Target, storePlugins, newCertificate, oldCertificate))
                            {   
                                // This is not truly fatal, other installation plugins might still be able to do
                                // something useful, and also we don't want to break compatiblitiy for users depending
                                // on scripts that return an error
                                context.Result.AddErrorMessage($"Installation plugin {installOptions.Name} encountered an error");
                            }
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
                    if (storePluginOptions[i].KeepExisting != true &&
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
    
    }
}
