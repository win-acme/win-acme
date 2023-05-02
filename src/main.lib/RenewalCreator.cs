using ACMESharp.Protocol.Resources;
using Autofac;
using Autofac.Core;
using MimeKit.Cryptography;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base;
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
        private readonly MainArguments _mainArgs;
        private readonly AccountArguments _accountArgs;
        private readonly IPluginService _plugin;
        private readonly ISharingLifetimeScope _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly DueDateStaticService _dueDate;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecution;
        private readonly IValidationOptionsService _validation;
        private readonly NotificationService _notification;
        private readonly AccountManager _accountManager;

        public RenewalCreator(
            MainArguments mainArgs, AccountArguments accountArgs,
            IRenewalStore renewalStore, ISharingLifetimeScope container,
            IInputService input, ILogService log,
            IPluginService plugin, IAutofacBuilder autofacBuilder,
            IValidationOptionsService validationOptions, AccountManager accountManager,
            NotificationService notification, DueDateStaticService dueDateService,
            ExceptionHandler exceptionHandler, RenewalExecutor renewalExecutor)
        {
            _renewalStore = renewalStore;
            _input = input;
            _log = log;
            _validation = validationOptions;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecution = renewalExecutor;
            _notification = notification;
            _dueDate = dueDateService;
            _plugin = plugin;
            _mainArgs = mainArgs;
            _accountArgs = accountArgs;
            _accountManager = accountManager;
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
            if (existing == null && string.IsNullOrEmpty(_mainArgs.Id))
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
            existing.Account = temp.Account;
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
            if (_mainArgs.Test)
            {
                runLevel |= RunLevel.Test;
            }
            if (_mainArgs.NoCache)
            {
                runLevel |= RunLevel.NoCache;
            }
            _log.Information(LogType.All, "Running in mode: {runLevel}", runLevel);

            tempRenewal ??= Renewal.Create(_mainArgs.Id);

            // Choose the target plugin
            var resolver = CreateResolver(_container, runLevel);
            if (steps.HasFlag(Steps.Source))
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
            if (!initialTarget.IsValid(_log, false))
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginName} generated an invalid source.");
                return;
            }
            _log.Information("Source generated using plugin {name}: {target}", targetPluginName, initialTarget);

            // Setup the friendly name
            var ask = runLevel.HasFlag(RunLevel.Advanced | RunLevel.Interactive) && steps.HasFlag(Steps.Source);
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
                // We only need to pick validation for those identifiers that
                // do not have global options configured. 
                var allIdentifiers = initialTarget.Parts.SelectMany(x => x.Identifiers).Distinct().Order().ToList();
                var mapping = allIdentifiers.ToDictionary(x => x, x => (PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>?)null);
                foreach (var identifier in allIdentifiers)
                {
                    var options = await _validation.GetValidationOptions(identifier);
                    if (options != null)
                    {
                        var pluginFrontend = _scopeBuilder.ValidationFrontend(targetPluginScope, options, identifier);
                        _log.Debug("Global validation option {name} found for {identifier}", pluginFrontend.Meta.Name, identifier.Value);
                        var state = pluginFrontend.Capability.State;
                        if (!state.Disabled)
                        {
                            mapping[identifier] = pluginFrontend;
                        }
                        else
                        {
                            _log.Warning("Global validation {name} disabled: {state}", pluginFrontend.Meta.Name, state.Reason);
                        }
                    } 
                    else
                    {
                        _log.Verbose("Global validation option not found for {identifier}", identifier.Value);
                    }
                }
                var withGlobalOptions = mapping.Where(x => x.Value != null).Select(x => x.Key).ToList(); ;
                var withoutGlobalOptions = allIdentifiers.Except(withGlobalOptions).ToList();
                var validationResolver = resolver;

                // If everything is covered by global options, we don't want
                // to bother the user with for their preference anymore in
                // simple/interactive mode. But in unatteded or advanced mode
                // we do still want to configure the renewal-local settings,
                // so that those will be used as a fallback when needed, 
                // either due to a change in the source or a change in the 
                // global options.
                if (withGlobalOptions.Any())
                {
                    _input.CreateSpace();
                    _input.Show(null, $"Note: {withGlobalOptions.Count} of {allIdentifiers.Count} " +
                        $"identifiers found in the source are covered by usable global validation options. " +
                        $"Any validation settings configured for the renewal will only apply to the " +
                        $"remainder.");
                    await _input.WritePagedList(allIdentifiers.Select(identifier => 
                        Choice.Create(
                            identifier, 
                            $"{identifier.Value}: {mapping[identifier]?.Meta.Name ?? "-"}{(mapping[identifier]?.Capability.State.Disabled ?? false ? " (disabled)" : "")}")));
                    _input.CreateSpace();
                }

                if (withGlobalOptions.Count > 0 && withoutGlobalOptions.Count > 0)
                {
                    // While picking the validation plugin for the remaining identifiers
                    // not covered by the global validation options, we should not be 
                    // restricted by rules that apply to the covered identifiers. 
                    // E.g. when a wildcard domain like *.example.com is covered by a
                    // global DNS validation setting, we should be able to pick a 
                    // HTTP validation plugin for www.example.com
                    var filteredTarget = new Target(withoutGlobalOptions);
                    var filteredScope = _scopeBuilder.Target(targetPluginScope, filteredTarget);
                    validationResolver = CreateResolver(filteredScope, runLevel);
                } 
                else if (withoutGlobalOptions.Count == 0)
                {
                    // If all source identifiers are already covered by the global
                    // options, we want to create a universal target that could
                    // potentially fit validation plugin.
                    var filteredTarget = new Target(new DnsIdentifier("www.example.com"));
                    var filteredScope = _scopeBuilder.Target(targetPluginScope, filteredTarget);
                    validationResolver = CreateResolver(filteredScope, runLevel);
                }

                var validationOptions = await SetupValidation(validationResolver, runLevel);
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

            // Choose under which account the renewals should run
            if (steps.HasFlag(Steps.Account))
            {
                tempRenewal.Account = await SetupAccount(runLevel);
            }

            // Try to run for the first time
            var renewal = await CreateRenewal(tempRenewal, runLevel);
            var retry = true;
            while (retry)
            {
                retry = await FirstRun(renewal, runLevel);
            }
        }

        /// <summary>
        /// First run, with several user escapes before the renewal 
        /// becomes final.
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<bool> FirstRun(Renewal renewal, RunLevel runLevel)
        {
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
                    return true;
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
            return false;
        }

        /// <summary>
        /// Choose friendly name to use for the PFX file
        /// </summary>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task SetupFriendlyName(Renewal renewal, Target target, bool ask)
        {
            if (!string.IsNullOrEmpty(_mainArgs.FriendlyName))
            {
                renewal.FriendlyName = _mainArgs.FriendlyName;
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

        private async Task<ValidationPluginOptions?> SetupValidation(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Validation, runLevel, resolver.GetValidationPlugin);

        private async Task<OrderPluginOptions?> SetupOrder(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Order, runLevel, resolver.GetOrderPlugin);

        private async Task<TargetPluginOptions?> SetupTarget(IResolver resolver, RunLevel runLevel) =>
            await SetupPlugin(Steps.Source, runLevel, resolver.GetTargetPlugin);

        private async Task<CsrPluginOptions?> SetupCsr(IResolver resolver, RunLevel runLevel) => 
            await SetupPlugin(Steps.Csr, runLevel, resolver.GetCsrPlugin);

        private async Task<List<StorePluginOptions>?> SetupStore(IResolver resolver, RunLevel runLevel) =>
            await SetupPlugins(Steps.Store, runLevel, resolver.GetStorePlugin, typeof(Plugins.StorePlugins.Null));

        private async Task<List<InstallationPluginOptions>?> SetupInstallation(IResolver resolver, RunLevel runLevel, Renewal renewal)
        {
            var stores = renewal.StorePluginOptions.Select(_plugin.GetPlugin);
            return await SetupPlugins(Steps.Installation, runLevel, factories => resolver.GetInstallationPlugin(stores, factories), typeof(Plugins.InstallationPlugins.Null));
        }

        private async Task<string?> SetupAccount(RunLevel runLevel)
        {
            // Unattended only listens to the command line
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                if (_accountArgs.Account != null)
                {
                    _log.Information("Using account {name}", _accountArgs.Account);
                    return _accountArgs.Account;
                }
                return null;
            }

            // So we are interactive...
            var accounts = _accountManager.ListAccounts().ToList();
            if (_accountArgs.Account != null && !accounts.Contains(_accountArgs.Account))
            {
                accounts.Add(_accountArgs.Account);
            }
            var selected = _accountArgs.Account ?? accounts.FirstOrDefault();
            if (runLevel.HasFlag(RunLevel.Advanced))
            {
                if (accounts.Count > 1)
                {
                    return await _input.ChooseRequired(
                        "Choose ACME account to use", 
                        accounts, 
                        x => new Choice<string>(x) { 
                            Description = x == "" ? "Default account" : $"Named account: {x}",
                            Default = string.Equals(x, selected, StringComparison.OrdinalIgnoreCase),
                        });
                }
            }
            if (selected == "")
            {
                selected = null;
            }
            return selected;
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
        private async Task<List<TOptions>?> SetupPlugins<TOptions, TCapability>(
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
                _exceptionHandler.HandleException(message: $"{step} plugin could not be selected");
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
                // Ensure that cache keys calculated based on 
                // plugin options are stable. If the ID is only
                // added during serialisation it might be too late.
                options.Plugin = plugin.Meta.Id.ToString();
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