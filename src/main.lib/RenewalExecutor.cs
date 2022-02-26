using Autofac;
using Newtonsoft.Json.Schema;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
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
        private readonly ISettingsService _settings;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalValidator _validator;

        public RenewalExecutor(
            MainArguments args,
            IAutofacBuilder scopeBuilder,
            ILogService log,
            IInputService input,
            ISettingsService settings,
            RenewalValidator validator,
            ExceptionHandler exceptionHandler, 
            IContainer container)
        {
            _validator = validator;
            _args = args;
            _scopeBuilder = scopeBuilder;
            _log = log;
            _input = input;
            _settings = settings;
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

            // Create one or more orders based on the target
            var orderPlugin = es.Resolve<IOrderPlugin>();
            var orders = orderPlugin.Split(renewal, target);
            if (orders == null || !orders.Any())
            {
                return new RenewResult("Order plugin failed to create order(s)");
            }
            _log.Verbose("Targeted convert into {n} order(s)", orders.Count());

            // Test if this renewal should run right now
            if (!ShouldRun(es, orders, renewal, runLevel))
            {
                return new RenewResult() { Abort = true };
            }

            // If at this point we haven't retured already with an error/abort
            // actually execute the renewal
            var preScript = _settings.Execution?.DefaultPreExecutionScript;
            var scriptClient = es.Resolve<ScriptClient>();
            if (!string.IsNullOrWhiteSpace(preScript))
            {
                await scriptClient.RunScript(preScript, $"{renewal.Id}");
            }
            var result = await ExecuteRenewal(es, orders.ToList(), runLevel);
            var postScript = _settings.Execution?.DefaultPostExecutionScript;
            if (!string.IsNullOrWhiteSpace(postScript))
            {
                await scriptClient.RunScript(postScript, $"{renewal.Id}");
            }

            // Configure task scheduler
            var setupTaskScheduler = _args.SetupTaskScheduler;
            if (!setupTaskScheduler && !_args.NoTaskScheduler)
            {
                setupTaskScheduler = result.Success && !result.Abort && (renewal.New || renewal.Updated);
            }
            if (setupTaskScheduler && runLevel.HasFlag(RunLevel.Test))
            {
                setupTaskScheduler = await _input.PromptYesNo($"[--test] Do you want to automatically renew with these settings?", true);
                if (!setupTaskScheduler)
                {           
                    result.Abort = true;
                }
            }
            if (setupTaskScheduler)
            {
                var taskLevel = runLevel;
                if (_args.SetupTaskScheduler)
                {
                    taskLevel |= RunLevel.ForceRenew;
                }
                await es.Resolve<TaskSchedulerService>().EnsureTaskScheduler(runLevel);
            }
            return result;
        }

        private bool ShouldRun(ILifetimeScope target, IEnumerable<Order> orders, Renewal renewal, RunLevel runLevel)
        {
            // Check if renewal is needed
            if (!runLevel.HasFlag(RunLevel.ForceRenew) && !renewal.Updated)
            {
                _log.Verbose("Checking {renewal}", renewal.LastFriendlyName);
                if (!renewal.IsDue())
                {
                    var cs = target.Resolve<ICertificateService>();
                    var abort = true;
                    foreach (var order in orders)
                    {
                        var cache = cs.CachedInfo(order);
                        if (cache == null && !renewal.New)
                        {
                            _log.Information(LogType.All, "Renewal {renewal} running prematurely due to source change", renewal.LastFriendlyName);
                            abort = false;
                            break;
                        }
                    }
                    if (abort)
                    {
                        _log.Information("Renewal {renewal} is due after {date}", renewal.LastFriendlyName, renewal.GetDueDate());
                        return false;
                    }
                }
                else if (!renewal.New)
                {
                    _log.Information(LogType.All, "Renewing certificate {renewal}", renewal.LastFriendlyName);
                }
            }
            else if (runLevel.HasFlag(RunLevel.ForceRenew))
            {
                _log.Information(LogType.All, "Force renewing certificate {renewal}", renewal.LastFriendlyName);
            }
            return true;
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

                // Create the execution context
                var context = new ExecutionContext(execute, order, runLevel, result);
                await ExecuteOrder(context, runLevel);
            }
            return result;
        }

        /// <summary>
        /// Steps to take on succesful (re)authorization
        /// </summary>
        /// <param name="partialTarget"></param>
        private async Task ExecuteOrder(ExecutionContext context, RunLevel runLevel)
        {
            try
            {
                // Get the previously issued certificate in this renewal
                // regardless of the fact that it may have another shape
                // (e.g. different SAN names or common name etc.). This
                // means we cannot use the cache key for it.
                var certificateService = context.Scope.Resolve<ICertificateService>();

                // Before we do anything, lock down the previously issued 
                // certificate
                var previousCertificate = certificateService.
                    CachedInfos(context.Renewal).
                    OrderByDescending(x => x.Certificate.NotBefore).
                    FirstOrDefault();

                // Get the existing certificate matching the order description
                // this may not be the same as the previous certificate
                var newCertificate = 
                    GetFromCache(context, runLevel) ??
                    await GetFromServer(context, runLevel);
                if (newCertificate == null)
                {
                    context.Result.AddErrorMessage("No certificate generated");
                    return;
                }
  
                // Store the date the certificate will expire
                if (context.Result.ExpireDate == null ||
                    context.Result.ExpireDate > newCertificate.Certificate.NotAfter)
                {
                    context.Result.ExpireDate = newCertificate.Certificate.NotAfter;
                };
                context.Result.AddThumbprint(newCertificate.Certificate.Thumbprint);

                // Early escape for testing validation only
                if (context.Renewal.New &&
                    context.RunLevel.HasFlag(RunLevel.Test) &&
                    !await _input.PromptYesNo($"[--test] Do you want to install the certificate?", true))
                {
                    context.Result.Abort = true;
                    return;
                }

                // Load the store plugins
                var storePluginOptions = context.Renewal.StorePluginOptions.
                    Where(x => !(x is NullStoreOptions)).
                    ToList();
                var storePlugins = storePluginOptions.
                    Select(x => context.Scope.Resolve(x.Instance, new TypedParameter(x.GetType(), x))).
                    OfType<IStorePlugin>().
                    Where(x => !(x is INull)).
                    ToList();
                if (storePluginOptions.Count != storePlugins.Count)
                {
                    throw new InvalidOperationException("Store plugin/option count mismatch");
                }

                if (!await HandleStoreAdd(context, newCertificate, storePluginOptions, storePlugins)) 
                {
                    return;
                }
                if (!await HandleInstall(context, newCertificate, previousCertificate, storePlugins))
                {
                    return;
                }
                if (previousCertificate != null && 
                    newCertificate.Certificate.Thumbprint != previousCertificate.Certificate.Thumbprint)
                {
                    await HandleStoreRemove(context, previousCertificate, storePluginOptions, storePlugins);
                }
            }
            catch (Exception ex)
            {
                var message = _exceptionHandler.HandleException(ex);
                context.Result.AddErrorMessage(message);
            }
        }

        /// <summary>
        /// Get a certificate from the cache
        /// </summary>
        /// <param name="context"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private CertificateInfo? GetFromCache(ExecutionContext context, RunLevel runLevel)
        {
            var certificateService = context.Scope.Resolve<ICertificateService>();
            var cachedCertificate = certificateService.CachedInfo(context.Order);
            if (cachedCertificate == null || cachedCertificate.CacheFile == null)
            {
                return null;
            }
            if (cachedCertificate.CacheFile.LastWriteTime < DateTime.Now.AddDays(_settings.Cache.ReuseDays * -1))
            {
                return null;
            }
            if (runLevel.HasFlag(RunLevel.IgnoreCache))
            {
                _log.Warning(
                    "Cached certificate available but not used due to --{switch} switch.",
                    nameof(MainArguments.Force).ToLower());
                return null;
            }
            _log.Warning(
                "Using cache for {friendlyName}. To get a new certificate " +
                "within {days} days, run with --{switch}.",
                context.Order.FriendlyNameIntermediate,
                _settings.Cache.ReuseDays,
                nameof(MainArguments.Force).ToLower());
                return cachedCertificate;
        }


        private async Task<CertificateInfo?> GetFromServer(ExecutionContext context, RunLevel runLevel)
        {
            // Place the order
            var certificateService = context.Scope.Resolve<ICertificateService>();
            var orderManager = context.Scope.Resolve<OrderManager>();
            context.Order.KeyPath = context.Order.Renewal.CsrPluginOptions?.ReusePrivateKey == true
                ? certificateService.ReuseKeyPath(context.Order) : null;
            context.Order.Details = await orderManager.GetOrCreate(context.Order, runLevel);

            // Sanity checks
            if (context.Order.Details == null)
            {
                context.Result.AddErrorMessage($"Unable to create order");
                return null;
            }
            if (context.Order.Details.Payload.Status == AcmeClient.OrderInvalid)
            {
                context.Result.AddErrorMessage($"Created order was invalid");
                return null;
            }

            // Generate the CSR plugin
            var csrPlugin = context.Target.CsrBytes == null ? context.Scope.Resolve<ICsrPlugin>() : null;
            if (csrPlugin != null)
            {
                var (disabled, disabledReason) = csrPlugin.Disabled;
                if (disabled)
                {
                    context.Result.AddErrorMessage($"CSR plugin is not available. {disabledReason}");
                    return null;
                }
            }

            // Run validations
            if (!context.Order.Valid || runLevel.HasFlag(RunLevel.Test) || runLevel.HasFlag(RunLevel.IgnoreCache))
            {
                await _validator.AuthorizeOrder(context);
                if (!context.Result.Success)
                {
                    return null;
                }
            }

            // Request the certificate
            return await certificateService.RequestCertificate(csrPlugin, context.RunLevel, context.Order);
        }

        /// <summary>
        /// Handle store plugins
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newCertificate"></param>
        /// <returns></returns>
        private async Task<bool> HandleStoreAdd(
            ExecutionContext context, 
            CertificateInfo newCertificate, 
            List<StorePluginOptions> storePluginOptions, 
            List<IStorePlugin> storePlugins)
        {
            // Run store plugin(s)
            try
            {
                var steps = storePluginOptions.Count;
                for (var i = 0; i < steps; i++)
                {
                    var storeOptions = storePluginOptions[i];
                    var storePlugin = storePlugins[i];
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
                        return false;
                    } 
                    await storePlugin.Save(newCertificate);
                }
            }
            catch (Exception ex)
            {
                var reason = _exceptionHandler.HandleException(ex, "Unable to store certificate");
                context.Result.AddErrorMessage($"Store failed: {reason}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Remove previous certificate from store
        /// </summary>
        /// <param name="context"></param>
        /// <param name="previousCertificate"></param>
        /// <param name="storePluginOptions"></param>
        /// <param name="storePlugins"></param>
        /// <returns></returns>
        private async Task HandleStoreRemove(
            ExecutionContext context,
            CertificateInfo previousCertificate,
            List<StorePluginOptions> storePluginOptions,
            List<IStorePlugin> storePlugins)
        {
            for (var i = 0; i < storePluginOptions.Count; i++)
            {
                if (storePluginOptions[i].KeepExisting != true)
                {
                    try
                    {
                        await storePlugins[i].Delete(previousCertificate);
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

        /// <summary>
        /// Handle installation steps
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newCertificate"></param>
        /// <param name="previousCertificate"></param>
        /// <returns></returns>
        private async Task<bool> HandleInstall(
            ExecutionContext context,
            CertificateInfo newCertificate, 
            CertificateInfo? previousCertificate, 
            IEnumerable<IStorePlugin> storePlugins)
        {
            // Run installation plugin(s)
            try
            {
                var steps = context.Renewal.InstallationPluginOptions.Count;
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
                            return false;
                        }
                        if (!await installPlugin.Install(context.Target, storePlugins, newCertificate, previousCertificate))
                        {
                            // This is not truly fatal, other installation plugins might still be able to do
                            // something useful, and also we don't want to break compatability for users depending
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
                return false;
            }
            return true;
        }
    }
}
