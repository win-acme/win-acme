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
        private readonly ICertificateService _certificateService;
        private readonly IDueDateService _dueDate;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalValidator _validator;

        public RenewalExecutor(
            MainArguments args,
            IAutofacBuilder scopeBuilder,
            ILogService log,
            IInputService input,
            ISettingsService settings,
            ICertificateService certificateService,
            IDueDateService dueDate,
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
            _certificateService = certificateService;
            _container = container;
            _dueDate = dueDate;
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
            _log.Verbose("Source converted into {n} order(s)", orders.Count());

            /// Start to check the renewal
            var result = await HandleOrders(es, renewal, orders.ToList(), runLevel);

            // Configure task scheduler
            var setupTaskScheduler = _args.SetupTaskScheduler;
            if (!setupTaskScheduler && !_args.NoTaskScheduler)
            {
                setupTaskScheduler = result.Success == true && !result.Abort && (renewal.New || renewal.Updated);
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

        /// <summary>
        /// Test if a renewal is needed
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal bool ShouldRunRenewal(Renewal renewal, RunLevel runLevel)
        {
            if (renewal.New)
            {
                return true;
            }
            if (!runLevel.HasFlag(RunLevel.ForceRenew) && !renewal.Updated)
            {
                _log.Verbose("Checking {renewal}", renewal.LastFriendlyName);
                if (!_dueDate.ShouldRun(renewal))
                {
                    return false;
                }
            }
            else if (runLevel.HasFlag(RunLevel.ForceRenew))
            {
                _log.Information(LogType.All, "Force renewing {renewal}", renewal.LastFriendlyName);
            }
            return true;
        }

        /// <summary>
        /// Return abort result
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        internal RenewResult Abort(Renewal renewal)
        {
            var dueDate = _dueDate.DueDate(renewal);
            if (dueDate != null)
            {
                // For sure now that we don't need to run so abort this execution
                _log.Information("Renewal {renewal} is due after {date}", renewal.LastFriendlyName, _input.FormatDate(dueDate.Value));
            }
            return new RenewResult() { Abort = true };
        }

        /// <summary>
        /// Run the renewal 
        /// </summary>
        /// <param name="execute"></param>
        /// <param name="orders"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<RenewResult> HandleOrders(ILifetimeScope execute, Renewal renewal, List<Order> orders, RunLevel runLevel)
        {
            // Build context
            var result = new RenewResult();
            var orderContexts = orders.Select(order => new OrderContext(execute, order, runLevel, _dueDate.ShouldRun(order), result)).ToList();

            // Check if renewal is needed at the root level
            if (!ShouldRunRenewal(renewal, runLevel))
            {
                // If renewal is not needed at the root level
                // it may be needed at the order level due to
                // change in target. Here we check this.
                if (!orderContexts.Any(x => x.ShouldRun))
                {
                    return Abort(renewal);
                } 
            }

            // Only process orders that are due. In the normal
            // case when using static due dates this will be all 
            // the orders. But when using the random due dates,
            // this could only be a part of them.
            if (!runLevel.HasFlag(RunLevel.IgnoreCache) && !renewal.New)
            {
                orderContexts = orderContexts.Where(x => x.ShouldRun).ToList();
            }
            if (!orderContexts.Any())
            {
                _log.Debug("None of the orders are currently due to run", orderContexts.Count, orders.Count);
                return Abort(renewal);
            } 
            if (!renewal.New)
            {
                _log.Information(LogType.All, "Renewing {renewal}", renewal.LastFriendlyName);
            }
            if (orders.Count > orderContexts.Count)
            {
                _log.Information("{n} of {m} orders are due to run", orderContexts.Count, orders.Count);
            }
            else
            {
                _log.Debug("{n} of {m} orders are due to run", orderContexts.Count, orders.Count);
            }

            // If at this point we haven't retured already with an error/abort
            // actually execute the renewal

            // Run the pre-execution script, e.g. to re-configure
            // local firewall rules, since now it's (almost) sure
            // that we're going to do something. Actually we may
            // still be able to read all certificates from cache,
            // but that's the exception rather than the rule.
            var preScript = _settings.Execution?.DefaultPreExecutionScript;
            var scriptClient = execute.Resolve<ScriptClient>();
            if (!string.IsNullOrWhiteSpace(preScript))
            {
                await scriptClient.RunScript(preScript, $"{renewal.Id}");
            }


            // Get the certificates from cache or server
            await ExecuteOrders(orderContexts, runLevel);

            // Handle all the store/install steps
            await ProcessOrders(orderContexts);

            // Run the post-execution script. Note that this is different
            // from the script installation plugin, which is handled
            // in the previous step. This is only meant to undo any
            // (firewall?) changes made by the pre-execution script.
            var postScript = _settings.Execution?.DefaultPostExecutionScript;
            if (!string.IsNullOrWhiteSpace(postScript))
            {
                await scriptClient.RunScript(postScript, $"{renewal.Id}");
            }

            // Return final result
            return result;
        }

        /// <summary>
        /// Get the certificates, if not from cache then from the server
        /// </summary>
        /// <param name="orderContexts"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task ExecuteOrders(List<OrderContext> orderContexts, RunLevel runLevel)
        {
            foreach (var order in orderContexts)
            {
                // Get the previously issued certificates in this renewal
                // sub order regardless of the fact that it may have another
                // shape (e.g. different SAN names or common name etc.). This
                // means we cannot use the cache key for it.
                order.PreviousCertificate = _certificateService.
                    CachedInfos(order.Renewal, order.Order).
                    OrderByDescending(x => x.Certificate.NotBefore).
                    FirstOrDefault();

                // Get the existing certificate matching the order description
                // this may not be the same as the previous certificate
                order.NewCertificate = GetFromCache(order, runLevel);
            }

            // Group validations of multiple order together
            // as to maximize the potential gains in parallelization
            var fromServer = orderContexts.Where(x => x.NewCertificate == null).ToList();
            foreach (var order in fromServer)
            {
                await CreateOrder(order);
            }

            // Validate all orders that need it
            var alwaysTryValidation = runLevel.HasFlag(RunLevel.Test) || runLevel.HasFlag(RunLevel.IgnoreCache);
            var validationRequired = fromServer.Where(x => x.Order.Details != null && (x.Order.Valid == false || alwaysTryValidation));
            await _validator.AuthorizeOrders(validationRequired, runLevel);

            // Run validations for order that couldn't be retrieved from cache
            foreach (var order in orderContexts)
            {
                if (order.Result.Success == false)
                {
                    _log.Verbose("Order {n}/{m} ({friendly}): validation error",
                         orderContexts.IndexOf(order) + 1,
                         orderContexts.Count,
                         order.OrderName);
                } 
                else if (order.NewCertificate == null)
                {
                    _log.Verbose("Order {n}/{m} ({friendly}): processing...",
                         orderContexts.IndexOf(order) + 1,
                         orderContexts.Count,
                         order.OrderName);
                    // Get the certificate from the server
                    order.NewCertificate = await GetFromServer(order);
                }
                else
                {
                    _log.Verbose("Order {n}/{m} ({friendly}): handle from cache",
                         orderContexts.IndexOf(order) + 1,
                         orderContexts.Count,
                         order.OrderName);
                }
            }
        }

        /// <summary>
        /// Handle install/store steps
        /// </summary>
        /// <param name="orderContexts"></param>
        /// <returns></returns>
        private async Task ProcessOrders(List<OrderContext> orderContexts)
        {
            // Process store/install steps
            foreach (var order in orderContexts)
            {
                _log.Verbose("Processing order {n}/{m}: {friendly}",
                   orderContexts.IndexOf(order) + 1,
                   orderContexts.Count,
                   order.OrderName);

                if (order.NewCertificate == null)
                {
                    order.Result.AddErrorMessage($"No certificate generated for order {order.OrderName}");
                    continue;
                }

                // Store the date the certificate will expire
                if (order.Result.ExpireDate == null ||
                    order.Result.ExpireDate > order.NewCertificate.Certificate.NotAfter)
                {
                    order.Result.ExpireDate = order.NewCertificate.Certificate.NotAfter;
                };
                order.Result.AddThumbprint(order.NewCertificate.Certificate.Thumbprint);

                // Create the execution context
                await ProcessOrder(order);

                // Don't process the rest of the orders if one of them fails
                if (order.Result.Abort || order.Result.Success == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Run a single order that's part of the renewal 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task ProcessOrder(OrderContext context)
        {
            try
            {
                if (context.NewCertificate == null)
                {
                    throw new InvalidOperationException();
                }

                // Early escape for testing validation only
                if (context.Renewal.New &&
                    context.RunLevel.HasFlag(RunLevel.Test) &&
                    !await _input.PromptYesNo($"[--test] Do you want to install the certificate for order {context.OrderName}?", true))
                {
                    context.Result.Abort = true;
                    return;
                }

                // Load the store plugins
                var storePluginOptions = context.Renewal.StorePluginOptions.
                    Where(x => x is not NullStoreOptions).
                    ToList();
                var storePlugins = storePluginOptions.
                    Select(x => context.ExecutionScope.Resolve(x.Instance, new TypedParameter(x.GetType(), x))).
                    OfType<IStorePlugin>().
                    Where(x => x is not INull).
                    ToList();
                if (storePluginOptions.Count != storePlugins.Count)
                {
                    throw new InvalidOperationException("Store plugin/option count mismatch");
                }

                if (!await HandleStoreAdd(context, context.NewCertificate, storePluginOptions, storePlugins)) 
                {
                    return;
                }
                if (!await HandleInstall(context, context.NewCertificate, context.PreviousCertificate, storePlugins))
                {
                    return;
                }
                if (context.PreviousCertificate != null &&
                    context.NewCertificate.Certificate.Thumbprint != context.PreviousCertificate.Certificate.Thumbprint)
                {
                    await HandleStoreRemove(context, context.PreviousCertificate, storePluginOptions, storePlugins);
                }

                // Made it to the end!
                if (context.Result.Success == null)
                {
                    context.Result.Success = true;
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
        private CertificateInfo? GetFromCache(OrderContext context, RunLevel runLevel)
        {
            var cachedCertificate = _certificateService.CachedInfo(context.Order);
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

        /// <summary>
        /// Get the order from cache or place a new one at the server
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task CreateOrder(OrderContext context)
        {
            _log.Verbose("Obtain order details for {order}", context.OrderName);

            // Place the order
            var orderManager = context.ExecutionScope.Resolve<OrderManager>();
            context.Order.KeyPath = context.Order.Renewal.CsrPluginOptions?.ReusePrivateKey == true
                ? _certificateService.ReuseKeyPath(context.Order) : null;
            context.Order.Details = await orderManager.GetOrCreate(context.Order, context.RunLevel);

            // Sanity checks
            if (context.Order.Details == null)
            {
                context.Result.AddErrorMessage($"Unable to create order {context.OrderName}");
            }
            else if (context.Order.Details.Payload.Status == AcmeClient.OrderInvalid)
            {
                context.Result.AddErrorMessage($"Created order {context.OrderName} was invalid");
            }
        }

        /// <summary>
        /// Get a certificate from the server
        /// </summary>
        /// <param name="context"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<CertificateInfo?> GetFromServer(OrderContext context)
        {
            // Generate the CSR plugin
            var csrPlugin = context.Target.UserCsrBytes == null ? context.ExecutionScope.Resolve<ICsrPlugin>() : null;
            if (csrPlugin != null)
            {
                var (disabled, disabledReason) = csrPlugin.Disabled;
                if (disabled)
                {
                    context.Result.AddErrorMessage($"CSR plugin is not available. {disabledReason}");
                    return null;
                }
            }

            // Request the certificate
            return await _certificateService.RequestCertificate(csrPlugin, context.RunLevel, context.Order);
        }

        /// <summary>
        /// Handle store plugins
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newCertificate"></param>
        /// <returns></returns>
        private async Task<bool> HandleStoreAdd(
            OrderContext context, 
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
            OrderContext context,
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
            OrderContext context,
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
                    var installPlugin = (IInstallationPlugin)context.ExecutionScope.Resolve(
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
