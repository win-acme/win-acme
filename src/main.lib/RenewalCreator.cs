using Autofac;
using PKISharp.WACS.Configuration.Arguments;
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
    internal class RenewalCreator
    {
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly IRenewalStore _renewalStore;
        private readonly MainArguments _args;
        private readonly PasswordGenerator _passwordGenerator;
        private readonly ISettingsService _settings;
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
            ISettingsService settings, IAutofacBuilder autofacBuilder,
            NotificationService notification, IDueDateService dueDateService,
            ExceptionHandler exceptionHandler, RenewalExecutor renewalExecutor)
        {
            _passwordGenerator = passwordGenerator;
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _settings = settings;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecution = renewalExecutor;
            _notification = notification;
            _dueDate = dueDateService;
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
            if (_args.Force)
            {
                runLevel |= RunLevel.IgnoreCache;
            }
            _log.Information(LogType.All, "Running in mode: {runLevel}", runLevel);
            if (tempRenewal == null)
            {
                tempRenewal = Renewal.Create(_args.Id, _passwordGenerator);
            } 
            using var configScope = _scopeBuilder.Configuration(_container, tempRenewal, runLevel);

            // Choose the target plugin
            if (steps.HasFlag(Steps.Target))
            {
                var targetOptions = await SetupTarget(configScope, runLevel);
                if (targetOptions == null)
                {
                    return;
                }
                tempRenewal.TargetPluginOptions = targetOptions;
            }

            // Generate initial target
            using var targetScope = _scopeBuilder.Target(_container, tempRenewal, runLevel);
            var initialTarget = targetScope.Resolve<Target>();
            if (initialTarget is INull)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {tempRenewal.TargetPluginOptions.Name} was unable to generate the certificate parameters.");
                return;
            }
            if (!initialTarget.IsValid(_log))
            {
                _exceptionHandler.HandleException(message: $"Source plugin {tempRenewal.TargetPluginOptions.Name} generated invalid certificate parameters");
                return;
            }
            _log.Information("Source generated using plugin {name}: {target}", tempRenewal.TargetPluginOptions.Name, initialTarget);

            // Setup the friendly name
            var ask = runLevel.HasFlag(RunLevel.Advanced | RunLevel.Interactive) && steps.HasFlag(Steps.Target);
            await SetupFriendlyName(tempRenewal, initialTarget, ask);

            // Choose the validation plugin
            if (steps.HasFlag(Steps.Validation))
            {
                var validationOptions = await SetupValidation(targetScope, initialTarget, runLevel);
                if (validationOptions == null)
                {
                    return;
                }
                tempRenewal.ValidationPluginOptions = validationOptions;
            }

            // Choose the order plugin
            if (steps.HasFlag(Steps.Order))
            {
                tempRenewal.OrderPluginOptions = await SetupOrder(targetScope, runLevel);
                if (tempRenewal.OrderPluginOptions == null)
                {
                    return;
                }
            }

            // Choose the CSR plugin
            if (initialTarget.UserCsrBytes != null)
            {
                tempRenewal.CsrPluginOptions = null;
            }
            else if (steps.HasFlag(Steps.Csr))
            {
                tempRenewal.CsrPluginOptions = await SetupCsr(configScope, runLevel);
                if (tempRenewal.CsrPluginOptions == null)
                {
                    return;
                }
            }
            
            // Choose store plugin(s)
            if (steps.HasFlag(Steps.Store))
            {
                var store = await SetupStore(configScope, runLevel); 
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
                var install = await SetupInstallation(configScope, runLevel, tempRenewal, initialTarget);
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

        internal async Task<ValidationPluginOptions?> SetupValidation(ILifetimeScope scope, Target target, RunLevel runLevel) => 
            await SetupPlugin<ValidationPluginOptions, IValidationPluginOptionsFactory>(
                "Validation", scope, runLevel, x => x.Default(target), x => x.Aquire(target, _input, runLevel));

        internal async Task<OrderPluginOptions?> SetupOrder(ILifetimeScope scope, RunLevel runLevel) => 
            await SetupPlugin<OrderPluginOptions, IOrderPluginOptionsFactory>(
                "Order", scope, runLevel, x => x.Default(), x => x.Aquire(_input, runLevel));

        internal async Task<TargetPluginOptions?> SetupTarget(ILifetimeScope scope, RunLevel runLevel) =>
            await SetupPlugin<TargetPluginOptions, ITargetPluginOptionsFactory>(
                "Source", scope, runLevel, x => x.Default(), x => x.Aquire(_input, runLevel));

        internal async Task<CsrPluginOptions?> SetupCsr(ILifetimeScope scope, RunLevel runLevel) => 
            await SetupPlugin<CsrPluginOptions, ICsrPluginOptionsFactory>(
                "CSR", scope, runLevel, x => x.Default(), x => x.Aquire(_input, runLevel));

        internal async Task<List<StorePluginOptions>?> SetupStore(ILifetimeScope scope, RunLevel runLevel) =>
            await SetupPlugins<StorePluginOptions, IStorePluginOptionsFactory>(
                "Store",
                scope,
                runLevel,
                (resolver, factories) => resolver.GetStorePlugin(scope, factories),
                x => x.Default(),
                x => x.Aquire(_input, runLevel));

        internal async Task<List<InstallationPluginOptions>?> SetupInstallation(ILifetimeScope scope, RunLevel runLevel, Renewal renewal, Target target)
        {
            var stores = renewal.StorePluginOptions.Select(x => x.Instance);
            return await SetupPlugins<InstallationPluginOptions, IInstallationPluginOptionsFactory>(
                "Installation",
                scope,
                runLevel,
                (resolver, factories) => resolver.GetInstallationPlugin(scope, stores, factories),
                x => x.Default(target),
                x => x.Aquire(target, _input, runLevel));
        }

        /// <summary>
        /// Generic method to select a list of plugins
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <typeparam name="TOptionsFactory"></typeparam>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <param name="next"></param>
        /// <param name="default"></param>
        /// <param name="aquire"></param>
        /// <returns></returns>
        internal async Task<List<TOptions>?> SetupPlugins<TOptions, TOptionsFactory>(
            string name,
            ILifetimeScope scope, 
            RunLevel runLevel, 
            Func<IResolver, IEnumerable<TOptionsFactory>, Task<TOptionsFactory?>> next,
            Func<TOptionsFactory, Task<TOptions?>> @default,
            Func<TOptionsFactory, Task<TOptions?>> aquire)
            where TOptionsFactory : IPluginOptionsFactory
            where TOptions : class
        {
            var resolver = scope.Resolve<IResolver>();
            var ret = new List<TOptions>();
            var factories = new List<TOptionsFactory>();
            try
            {
                while (true)
                {
                    var factory = await next(resolver, factories);
                    if (factory == null)
                    {
                        _exceptionHandler.HandleException(message: $"{name} plugin could not be selected");
                        return null;
                    }
                    TOptions? options;
                    try
                    {
                        options = runLevel.HasFlag(RunLevel.Unattended)
                            ? await @default(factory)
                            : await aquire(factory);
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, $"{name} plugin {factory.Name} aborted or failed");
                        return null;
                    }
                    if (options == null)
                    {
                        _exceptionHandler.HandleException(message: $"{name} plugin {factory.Name} was unable to generate options");
                        return null;
                    }
                    var isNull = factory is INull;
                    if (!isNull || factories.Count == 0)
                    {
                        ret.Add(options);
                        factories.Add(factory);
                    }
                    if (isNull)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Invalid selection of {name} plugins");
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
        internal async Task<TOptions?> SetupPlugin<TOptions, TOptionsFactory>(
            string name,
            ILifetimeScope scope,
            RunLevel runLevel,
            Func<TOptionsFactory, Task<TOptions?>> @default,
            Func<TOptionsFactory, Task<TOptions?>> aquire)
            where TOptionsFactory : IPluginOptionsFactory
            where TOptions : class
        {
            // Choose the options factory
            var optionsFactory = scope.Resolve<TOptionsFactory>();
            if (optionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No {name} plugin could be selected");
                return null;
            }
            var (pluginDisabled, pluginDisabledReason) = optionsFactory.Disabled;
            if (pluginDisabled)
            {
                _exceptionHandler.HandleException(message: $"{name} plugin {optionsFactory.Name} is not available. {pluginDisabledReason}");
                return null;
            }

            // Configure the plugin
            try
            {
                var options = runLevel.HasFlag(RunLevel.Unattended) ?
                    await @default(optionsFactory) :
                    await aquire(optionsFactory); 
                if (options == null)
                {
                    _exceptionHandler.HandleException(message: $"{name} plugin {optionsFactory.Name} was unable to generate options");
                    return null;
                }
                return options;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"{name} plugin {optionsFactory.Name} aborted or failed");
                return null;
            }
        }
    }
}