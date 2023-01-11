using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS
{
    internal class RenewalCreator
    {
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly IRenewalStore _renewalStore;
        private readonly MainArguments _args;
        private readonly PasswordGenerator _passwordGenerator;
        private readonly IPluginService _plugin;
        private readonly IContainer _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly IDueDateService _dueDate;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecution;
        private readonly NotificationService _notification;

        public RenewalCreator(
            PasswordGenerator passwordGenerator, MainArguments args,
            IRenewalStore renewalStore, IContainer container,
            IInputService input, ILogService log,
            IPluginService plugin, IAutofacBuilder autofacBuilder,
            NotificationService notification, IDueDateService dueDateService,
            ExceptionHandler exceptionHandler, RenewalExecutor renewalExecutor)
        {
            _passwordGenerator = passwordGenerator;
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecution = renewalExecutor;
            _notification = notification;
            _dueDate = dueDateService;
            _plugin = plugin;
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task<Renewal> CreateRenewal(Renewal temp, RunLevel runLevel)
        {
            // First check by id
            var existing = _renewalStore.FindByArguments(temp.Id, null).FirstOrDefault();

            // If Id has been specified, we don't consider the Friendlyname anymore
            // So specifying --id becomes a way to create duplicate certificates
            // with the same --friendlyname in unattended mode.
            if (existing == null && string.IsNullOrEmpty(_args.Id))
            {
                existing = _renewalStore.FindByArguments(null, temp.LastFriendlyName?.EscapePattern()).FirstOrDefault();
            }

            // This will be a completely new renewal, no further processing needed
            if (existing == null)
            {
                return temp;
            }

            // Match found with existing certificate, determine if we want to overwrite
            // it or create it side by side with the current one.
            if (runLevel.HasFlag(RunLevel.Interactive) && (temp.Id != existing.Id) && temp.New)
            {
                _input.CreateSpace();
                _input.Show("Existing renewal", existing.ToString(_dueDate, _input));
                if (!await _input.PromptYesNo($"Overwrite settings?", true))
                {
                    return temp;
                }
            }

            // Move settings from temporary renewal over to
            // the pre-existing one that we are overwriting
            _log.Warning("Overwriting previously created renewal");
            existing.Updated = true;
            existing.TargetPluginOptions = temp.TargetPluginOptions;
            existing.OrderPluginOptions = temp.OrderPluginOptions;
            existing.CsrPluginOptions = temp.CsrPluginOptions;
            existing.StorePluginOptions = temp.StorePluginOptions;
            existing.ValidationPluginOptions = temp.ValidationPluginOptions;
            existing.InstallationPluginOptions = temp.InstallationPluginOptions;
            return existing;
        }

        /// <summary>
        /// Setup a new scheduled renewal
        /// </summary>
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="steps"></param>
        /// <param name="tempRenewal"></param>
        /// <returns></returns>
        internal async Task SetupRenewal(RunLevel runLevel, Steps steps = Steps.All, Renewal? tempRenewal = null)
        {
            if (_args.Test)
            {
                runLevel |= RunLevel.Test;
            }
            if (_args.NoCache)
            {
                runLevel |= RunLevel.NoCache;
            }
            _log.Information(LogType.All, "Running in mode: {runLevel}", runLevel);

            tempRenewal ??= Renewal.Create(_args.Id, _passwordGenerator);

            // Choose the target plugin
            var resolver = CreateResolver(_container, runLevel);
            if (steps.HasFlag(Steps.Target))
            {
                var targetOptions = await SetupTarget(resolver, runLevel);
                if (targetOptions == null)
                {
                    return;
                }
                tempRenewal.TargetPluginOptions = targetOptions;
            }

            // Generate initial target
            using var targetPluginScope = _scopeBuilder.PluginBackend<ITargetPlugin, TargetPluginOptions>(_container, tempRenewal.TargetPluginOptions);
            var targetBackend = targetPluginScope.Resolve<ITargetPlugin>();
            var targetPluginName = targetPluginScope.Resolve<Plugin>().Name;
            var initialTarget = await targetBackend.Generate();
            if (initialTarget == null)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginName} was unable to generate the certificate parameters.");
                return;
            }
            if (!initialTarget.IsValid(_log))
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginName} generated invalid certificate parameters");
                return;
            }
            _log.Information("Source generated using plugin {name}: {target}", targetPluginName, initialTarget);

            // Setup the friendly name
            var ask = runLevel.HasFlag(RunLevel.Advanced | RunLevel.Interactive) && steps.HasFlag(Steps.Target);
            await SetupFriendlyName(tempRenewal, initialTarget, ask);

            // Create new resolver in a scope that knows
            // about the target so that other plugins can
            // make decisions based on that.
            var targetScope = _scopeBuilder.Target(targetPluginScope, initialTarget);
            resolver = CreateResolver(targetScope, runLevel);

            // Choose the order plugin
            if (steps.HasFlag(Steps.Order))
            {
                tempRenewal.OrderPluginOptions = await SetupOrder(resolver, runLevel);
                if (tempRenewal.OrderPluginOptions == null)
                {
                    return;
                }
            }

            // Choose the validation plugin
            if (steps.HasFlag(Steps.Validation))
            {
                var validationOptions = await SetupValidation(resolver, runLevel);
                if (validationOptions == null)
                {
                    return;
                }
                tempRenewal.ValidationPluginOptions = validationOptions;
            }

            // Choose the CSR plugin
            if (initialTarget.UserCsrBytes != null)
            {
                tempRenewal.CsrPluginOptions = null;
            }
            else if (steps.HasFlag(Steps.Csr))
            {
                tempRenewal.CsrPluginOptions = await SetupCsr(resolver, runLevel);
                if (tempRenewal.CsrPluginOptions == null)
                {
                    return;
                }
            }
            
            // Choose store plugin(s)
            if (steps.HasFlag(Steps.Store))
            {
                var store = await SetupStore(resolver, runLevel); 
                if (store != null)
                {
                    tempRenewal.StorePluginOptions = store;
                } 
                else
                {
                    return;
                }
            }

            // Choose installation plugin(s)
            if (steps.HasFlag(Steps.Installation))
            {
                var install = await SetupInstallation(resolver, runLevel, tempRenewal);
                if (install != null)
                {
                    tempRenewal.InstallationPluginOptions = install;
                }
                else
                {
                    return;
                }
            }

            // Try to run for the first time
            var renewal = await CreateRenewal(tempRenewal, runLevel);
        retry:
            var result = await _renewalExecution.HandleRenewal(renewal, runLevel);
            if (result.Abort)
            {
                _log.Information($"Create certificate cancelled");
            }
            else if (result.Success != true)
            {
                if (runLevel.HasFlag(RunLevel.Interactive) &&
                    await _input.PromptYesNo("Create certificate failed, retry?", false))
                {
                    goto retry;
                }
                if (!renewal.New && 
                    runLevel.HasFlag(RunLevel.Interactive) &&
                    await _input.PromptYesNo("Save these new settings anyway?", false))
                {
                    _renewalStore.Save(renewal, result);
                }
                _exceptionHandler.HandleException(message: $"Create certificate failed");
            }
            else
            {
                try
                {
                    _renewalStore.Save(renewal, result);
                    await _notification.NotifyCreated(renewal, _log.Lines);
                } 
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }

        /// <summary>
        /// Choose friendly name to use for the PFX file
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal async Task SetupFriendlyName(Renewal renewal, Target target, bool ask)
        {
            if (!string.IsNullOrEmpty(_args.FriendlyName))
            {
                renewal.FriendlyName = _args.FriendlyName;
            }
            else if (ask)
            {
                var current = renewal.FriendlyName ?? target.FriendlyName;
                var alt = await _input.RequestString($"Friendly name '{current}'. <Enter> to accept or type desired name");
                if (!string.IsNullOrWhiteSpace(alt))
                {
                    renewal.FriendlyName = alt;
                }
            }
            renewal.LastFriendlyName = renewal.FriendlyName ?? target.FriendlyName;
        }

        internal async Task<ValidationPluginOptions?> SetupValidation(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Validation, runLevel, resolver.GetValidationPlugin);

        internal async Task<OrderPluginOptions?> SetupOrder(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Order, runLevel, resolver.GetOrderPlugin);

        internal async Task<TargetPluginOptions?> SetupTarget(IResolver resolver, RunLevel runLevel) =>
            await SetupPlugin(Steps.Source, runLevel, resolver.GetTargetPlugin);

        internal async Task<CsrPluginOptions?> SetupCsr(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Csr, runLevel, resolver.GetCsrPlugin);

        internal async Task<List<StorePluginOptions>?> SetupStore(IResolver resolver, RunLevel runLevel) =>
            await SetupPlugins(Steps.Store, runLevel, resolver.GetStorePlugin, typeof(Plugins.StorePlugins.Null));

        internal async Task<List<InstallationPluginOptions>?> SetupInstallation(IResolver resolver, RunLevel runLevel, Renewal renewal)
        {
            var stores = renewal.StorePluginOptions.Select(_plugin.GetPlugin);
            return await SetupPlugins(Steps.Installation, runLevel, factories => resolver.GetInstallationPlugin(stores, factories), typeof(Plugins.InstallationPlugins.Null));
        }

        /// <summary>
        /// Generic method to select a list of plugins
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TCapability"></typeparam>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <param name="next"></param>
        /// <param name="default"></param>
        /// <param name="aquire"></param>
        /// <returns></returns>
        internal async Task<List<TOptions>?> SetupPlugins<TOptions, TCapability>(
            Steps step,
            RunLevel runLevel,
            Func<IEnumerable<Plugin>, Task<PluginFrontend<TCapability, TOptions>?>> next,
            Type nullType)
            where TCapability : IPluginCapability
            where TOptions : PluginOptions, new()
        {
            var ret = new List<TOptions>();
            var factories = new List<Plugin>();
            try
            {
                while (true)
                {
                    var plugin = await next(factories);
                    if (plugin == null)
                    {
                        _exceptionHandler.HandleException(message: $"{step} plugin could not be selected");
                        return null;
                    }
                    TOptions? options;
                    try
                    {
                        options = runLevel.HasFlag(RunLevel.Unattended)
                            ? await plugin.OptionsFactory.Default()
                            : await plugin.OptionsFactory.Aquire(_input, runLevel);
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, $"{step} plugin {plugin.Meta.Name} aborted or failed");
                        return null;
                    }
                    if (options == null)
                    {
                        _exceptionHandler.HandleException(message: $"{step} plugin {plugin.Meta.Name} was unable to generate options");
                        return null;
                    }
                    var isNull = plugin.Meta.Backend == nullType;
                    if (!isNull || factories.Count == 0)
                    {
                        ret.Add(options);
                        factories.Add(plugin.Meta);
                    }
                    if (isNull)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Invalid selection of {step} plugins");
            }
            return ret;
        }

        /// <summary>
        /// Generic method to pick a plugin
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <param name="default"></param>
        /// <param name="aquire"></param>
        /// <returns></returns>
        internal async Task<TOptions?> SetupPlugin<TOptions, TCapability>(
            Steps step,
            RunLevel runLevel,
            Func<Task<PluginFrontend<TCapability, TOptions>?>> resolve)
            where TCapability : IPluginCapability
            where TOptions : PluginOptions, new()
        {
            // Choose the plugin
            var plugin = await resolve();
            if (plugin == null)
            {
                _exceptionHandler.HandleException(message: $"No {step} plugin could be selected");
                return null;
            }
            // Configure the plugin
            try
            {
                var options = runLevel.HasFlag(RunLevel.Unattended) ?
                    await plugin.OptionsFactory.Default() :
                    await plugin.OptionsFactory.Aquire(_input, runLevel); 
                if (options == null)
                {
                    _exceptionHandler.HandleException(message: $"{step} plugin {plugin.Meta.Name} was unable to generate options");
                    return null;
                }
                return options;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"{step} plugin {plugin.Meta.Name} aborted or failed");
                return null;
            }
        }
    
        /// <summary>
        /// Create plugin resolver
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal static IResolver CreateResolver(ILifetimeScope scope, RunLevel runLevel)
        {
            // Create new resolver that includes the target
            // in the scope so that plugin system can make 
            // decisions based on its properties
            return runLevel.HasFlag(RunLevel.Interactive)
                ? scope.Resolve<InteractiveResolver>(new TypedParameter(typeof(ILifetimeScope), scope), new TypedParameter(typeof(RunLevel), runLevel))
                : scope.Resolve<UnattendedResolver>(new TypedParameter(typeof(ILifetimeScope), scope));
        }
    }
}