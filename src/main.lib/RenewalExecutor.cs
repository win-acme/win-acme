using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    /// <summary>
    /// This part of the code handles the actual creation/renewal 
    /// </summary>
    internal class RenewalExecutor
    {
        private readonly MainArguments _args;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ILifetimeScope _container;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ISettingsService _settings;
        private readonly IDueDateService _dueDate;
        private readonly TaskSchedulerService _taskScheduler;
        private readonly AcmeClient _acmeClient;

        public RenewalExecutor(
            MainArguments args,
            IAutofacBuilder scopeBuilder,
            ILogService log,
            IInputService input,
            ISettingsService settings,
            IDueDateService dueDate,
            TaskSchedulerService taskScheduler,
            AcmeClient acmeClient,
            ISharingLifetimeScope container)
        {
            _args = args;
            _scopeBuilder = scopeBuilder;
            _log = log;
            _input = input;
            _settings = settings;
            _container = container;
            _dueDate = dueDate;
            _taskScheduler = taskScheduler;
            _acmeClient = acmeClient;
        }

        /// <summary>
        /// Determine if the renewal should be executed
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public async Task<RenewResult> HandleRenewal(Renewal renewal, RunLevel runLevel)
        {
            _input.CreateSpace();
            _log.Reset();

            // Check the initial, combined target for the renewal
            var client = await _acmeClient.GetClient();
            using var es = _scopeBuilder.Execution(_container, renewal, client, runLevel);
            var targetPlugin = es.Resolve<PluginBackend<ITargetPlugin, IPluginCapability, TargetPluginOptions>>();
            if (targetPlugin.Capability.State.Disabled)
            {
                return new RenewResult($"Source plugin {targetPlugin.Meta.Name} is disabled. {targetPlugin.Capability.State.Reason}");
            }
            var target = await targetPlugin.Backend.Generate();
            if (target == null)
            {
                _log.Information("Plugin {targetPluginName} did not generate a source", targetPlugin.Meta.Name);
                return new RenewResult($"Plugin {targetPlugin.Meta.Name} did not generate a source");
            }
            _log.Information("Plugin {targetPluginName} generated source {common} with {n} identifiers",
                targetPlugin.Meta.Name, 
                target.CommonName.Value,
                target.Parts.SelectMany(p => p.Identifiers).Distinct().Count());

            // Create one or more orders from the target
            var targetScope = _scopeBuilder.Split(es, target);
            var orderPlugin = targetScope.Resolve<PluginBackend<IOrderPlugin, IPluginCapability, OrderPluginOptions>>();
            var orders = orderPlugin.Backend.Split(renewal, target).ToList();
            if (orders == null || !orders.Any())
            {
                return new RenewResult($"Order plugin {orderPlugin.Meta.Name} failed to create order(s)");
            }
            _log.Information($"Plugin {{order}} created {{n}} order{(orders.Count > 1?"s":"")}", orderPlugin.Meta.Name, orders.Count);
            foreach (var order in orders)
            {
                if (!order.Target.IsValid(_log))
                {
                    var blame = orders.Count > 1 ? "Order" : "Source";
                    var blamePlugin = orders.Count > 1 ? orderPlugin.Meta : targetPlugin.Meta;
                    return new RenewResult($"{blame} plugin {blamePlugin.Name} created invalid source");
                }
            }

            // Handle the orders
            var result = await HandleOrders(es, renewal, orders, runLevel);

            // Manage the task scheduler
            await ManageTaskScheduler(renewal, result, runLevel);

            return result;
        }

        /// <summary>
        /// Optionally ensure the task scheduler, depending on renewal result and 
        /// various other switches and settings.
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="result"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task ManageTaskScheduler(Renewal renewal, RenewResult result, RunLevel runLevel)
        {
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
                    taskLevel |= RunLevel.Force;
                }
                await _taskScheduler.EnsureTaskScheduler(taskLevel);
            }
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
            if (!runLevel.HasFlag(RunLevel.Force) && !renewal.Updated)
            {
                _log.Verbose("Checking {renewal}", renewal.LastFriendlyName);
                if (!_dueDate.ShouldRun(renewal))
                {
                    return false;
                }
            }
            else if (runLevel.HasFlag(RunLevel.Force))
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
            var orderContexts = orders.Select(order => new OrderContext(_scopeBuilder.Order(execute, order), order, runLevel)).ToList();

            // Check if renewal is needed at the root level
            var mainDue = ShouldRunRenewal(renewal, runLevel);

            // Check individual orders
            foreach (var o in orderContexts)
            {
                o.ShouldRun = runLevel.HasFlag(RunLevel.Force) || _dueDate.ShouldRun(o);
                _log.Verbose("Order {name} should run: {run}", o.OrderName, o.ShouldRun);
            }

            if (!mainDue)
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
            var allContexts = orderContexts;
            var runnableContexts = orderContexts;
            if (!runLevel.HasFlag(RunLevel.NoCache) && !renewal.New && !renewal.Updated)
            {
                runnableContexts = orderContexts.Where(x => x.ShouldRun).ToList();
            }
            if (!runnableContexts.Any())
            {
                _log.Debug("None of the orders are currently due to run");
                return Abort(renewal);
            }
            if (!renewal.New && !runLevel.HasFlag(RunLevel.Force))
            {
                _log.Information(LogType.All, "Renewing {renewal}", renewal.LastFriendlyName);
            }
            if (orders.Count > runnableContexts.Count)
            {
                _log.Information("{n} of {m} orders are due to run", runnableContexts.Count, orders.Count);
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
            var orderProcessor = execute.Resolve<OrderProcessor>();
            await orderProcessor.ExecuteOrders(runnableContexts, allContexts, runLevel);
            var result = new RenewResult
            {
                OrderResults = runnableContexts.Select(x => x.OrderResult).ToList()
            };
            // Handle all the store/install steps
            await orderProcessor.ProcessOrders(runnableContexts, result);

            // Run the post-execution script. Note that this is different
            // from the script installation pluginService, which is handled
            // in the previous step. This is only meant to undo any
            // (firewall?) changes made by the pre-execution script.
            var postScript = _settings.Execution?.DefaultPostExecutionScript;
            if (!string.IsNullOrWhiteSpace(postScript))
            {
                await scriptClient.RunScript(postScript, $"{renewal.Id}");
            }

            // Return final result
            result.Success = runnableContexts.All(o => o.OrderResult.Success == true);
            return result;
        }
    }
}
