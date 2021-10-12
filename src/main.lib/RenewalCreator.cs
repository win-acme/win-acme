using Autofac;
using PKISharp.WACS.Configuration.Arguments;
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
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecution;
        private readonly NotificationService _notification;

        public RenewalCreator(
            PasswordGenerator passwordGenerator, MainArguments args,
            IRenewalStore renewalStore, IContainer container,
            IInputService input, ILogService log, 
            ISettingsService settings, IAutofacBuilder autofacBuilder,
            NotificationService notification,
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
                _input.Show("Existing renewal", existing.ToString(_input));
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
                tempRenewal = Renewal.Create(_args.Id, _settings.ScheduledTask, _passwordGenerator);
            } 
            else
            {
                tempRenewal.InstallationPluginOptions.Clear();
                tempRenewal.StorePluginOptions.Clear();
            }
            using var configScope = _scopeBuilder.Configuration(_container, tempRenewal, runLevel);

            // Choose the target plugin
            if (steps.HasFlag(Steps.Target))
            {
                var targetPluginOptions = await SetupTarget(configScope, runLevel);
                if (targetPluginOptions == null)
                {
                    return;
                } 
                else
                {
                    tempRenewal.TargetPluginOptions = targetPluginOptions;
                }
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
                var validationPluginOptions = await SetupValidation(targetScope, initialTarget, runLevel);
                if (validationPluginOptions == null)
                {
                    return;
                }
                else
                {
                    tempRenewal.ValidationPluginOptions = validationPluginOptions;
                }
            }

            // Choose the order plugin
            if (steps.HasFlag(Steps.Order))
            {
                var orderPluginOptions = await SetupOrder(targetScope, runLevel);
                if (orderPluginOptions == null)
                {
                    return;
                }
                else
                {
                    tempRenewal.OrderPluginOptions = orderPluginOptions;
                }
            }

            // Choose the CSR plugin
            if (initialTarget.CsrBytes != null)
            {
                tempRenewal.CsrPluginOptions = null;
            }
            else if (steps.HasFlag(Steps.Csr))
            {
                var csrPluginOptions = await SetupCsr(configScope, runLevel);
                if (csrPluginOptions == null)
                {
                    return;
                }
                else
                {
                    tempRenewal.CsrPluginOptions = csrPluginOptions;
                }
            }
            
            // Choose store plugin(s)
            if (steps.HasFlag(Steps.Store))
            {
                var storePluginOptions = await SetupStore(configScope, runLevel);
                if (storePluginOptions == null)
                {
                    return;
                }
                else
                {
                    tempRenewal.StorePluginOptions = storePluginOptions;
                }
            }

            // Choose installation plugin(s)
            if (steps.HasFlag(Steps.Installation))
            {
                var installationPluginOptions = await SetupInstallation(configScope, runLevel, tempRenewal, initialTarget);
                if (installationPluginOptions == null)
                {
                    return;
                }
                else
                {
                    tempRenewal.InstallationPluginOptions = installationPluginOptions;
                }
            }

            // Try to run for the first time
            var renewal = await CreateRenewal(tempRenewal, runLevel);
        retry:
            var result = await _renewalExecution.HandleRenewal(renewal, runLevel);
            if (result.Abort)
            {
                _exceptionHandler.HandleException(message: $"Create certificate cancelled");
            }
            else if (!result.Success)
            {
                if (runLevel.HasFlag(RunLevel.Interactive) &&
                    await _input.PromptYesNo("Create certificate failed, retry?", false))
                {
                    goto retry;
                }
                if (!renewal.New && 
                    await _input.PromptYesNo("Save these new settings anyway?", false))
                {
                    _renewalStore.Save(renewal, result);
                }
                _exceptionHandler.HandleException(message: $"Create certificate failed: {string.Join("\n\t- ", result.ErrorMessages)}");
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
        /// Choose target plugin for the renewal
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <param name="tempRenewal"></param>
        /// <returns></returns>
        internal async Task<TargetPluginOptions?> SetupTarget(ILifetimeScope scope, RunLevel runLevel)
        {
            // Choose target plugin
            var targetPluginOptionsFactory = scope.Resolve<ITargetPluginOptionsFactory>();
            if (targetPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No source plugin could be selected");
                return null;
            }
            var (targetPluginDisabled, targetPluginDisabledReason) = targetPluginOptionsFactory.Disabled;
            if (targetPluginDisabled)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} is not available. {targetPluginDisabledReason}");
                return null;
            }
            var targetPluginOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                await targetPluginOptionsFactory.Default() :
                await targetPluginOptionsFactory.Aquire(_input, runLevel);
            if (targetPluginOptions == null)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} aborted or failed");
                return null;
            }
            return targetPluginOptions;
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

        /// <summary>
        /// Pick the validation plugin
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="target"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task<ValidationPluginOptions?> SetupValidation(ILifetimeScope scope, Target target, RunLevel runLevel)
        {
            // Choose validation plugin
            var validationPluginOptionsFactory = scope.Resolve<IValidationPluginOptionsFactory>();
            if (validationPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No validation plugin could be selected");
                return null;
            }

            // Configure validation
            try
            {
                var validationOptions = runLevel.HasFlag(RunLevel.Unattended)
                    ? await validationPluginOptionsFactory.Default(target)
                    : await validationPluginOptionsFactory.Aquire(target, _input, runLevel);
                if (validationOptions == null)
                {
                    _exceptionHandler.HandleException(message: $"Validation plugin {validationPluginOptionsFactory.Name} was unable to generate options");
                    return null;
                }
                return validationOptions;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Validation plugin {validationPluginOptionsFactory.Name} aborted or failed");
                return null;
            }
        }

        /// <summary>
        /// Pick the order plugin
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="target"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task<OrderPluginOptions?> SetupOrder(ILifetimeScope scope, RunLevel runLevel)
        {
            // Choose order plugin
            var orderPluginOptionsFactory = scope.Resolve<IOrderPluginOptionsFactory>();
            if (orderPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No order plugin could be selected");
                return null;
            }

            // Configure order
            try
            {
                var orderOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                    await orderPluginOptionsFactory.Default() :
                    await orderPluginOptionsFactory.Aquire(_input, runLevel);
                if (orderOptions == null)
                {
                    _exceptionHandler.HandleException(message: $"Order plugin {orderPluginOptionsFactory.Name} was unable to generate options");
                    return null;
                }
                return orderOptions;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Order plugin {orderPluginOptionsFactory.Name} aborted or failed");
                return null;
            }
        }

        /// <summary>
        /// Pick the CSR plugin
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task<CsrPluginOptions?> SetupCsr(ILifetimeScope scope, RunLevel runLevel)
        {
            // Choose CSR plugin
            var csrPluginOptionsFactory = scope.Resolve<ICsrPluginOptionsFactory>();
            if (csrPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No CSR plugin could be selected");
                return null;
            }

            // Configure CSR
            try
            {
                var csrOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                    await csrPluginOptionsFactory.Default() :
                    await csrPluginOptionsFactory.Aquire(_input, runLevel);
                if (csrOptions == null)
                {
                    _exceptionHandler.HandleException(message: $"CSR plugin {csrPluginOptionsFactory.Name} was unable to generate options");
                    return null;
                }
                return csrOptions;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"CSR plugin {csrPluginOptionsFactory.Name} aborted or failed");
                return null;
            }
        }

        /// <summary>
        /// Choose and configure store plugins
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        internal async Task<List<StorePluginOptions>?> SetupStore(ILifetimeScope scope, RunLevel runLevel)
        {
            var resolver = scope.Resolve<IResolver>();
            var storePluginOptionsFactories = new List<IStorePluginOptionsFactory>();
            var ret = new List<StorePluginOptions>();
            try
            {
                while (true)
                {
                    var storePluginOptionsFactory = await resolver.GetStorePlugin(scope, storePluginOptionsFactories);
                    if (storePluginOptionsFactory == null)
                    {
                        _exceptionHandler.HandleException(message: $"Store could not be selected");
                        continue;
                    }
                    StorePluginOptions? storeOptions;
                    try
                    {
                        storeOptions = runLevel.HasFlag(RunLevel.Unattended)
                            ? await storePluginOptionsFactory.Default()
                            : await storePluginOptionsFactory.Aquire(_input, runLevel);
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, $"Store plugin {storePluginOptionsFactory.Name} aborted or failed");
                        continue;
                    }
                    if (storeOptions == null)
                    {
                        _exceptionHandler.HandleException(message: $"Store plugin {storePluginOptionsFactory.Name} was unable to generate options");
                        continue;
                    }
                    var isNull = storePluginOptionsFactory is NullStoreOptionsFactory;
                    if (!isNull || storePluginOptionsFactories.Count == 0)
                    {
                        ret.Add(storeOptions);
                        storePluginOptionsFactories.Add(storePluginOptionsFactory);
                    }
                    if (isNull)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, "Invalid selection of store plugins");
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Choose and configure installation plugins
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="runLevel"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal async Task<List<InstallationPluginOptions>?> SetupInstallation(ILifetimeScope scope, RunLevel runLevel, Renewal renewal, Target target)
        {
            var resolver = scope.Resolve<IResolver>();
            var ret = new List<InstallationPluginOptions>();
            var installationPluginFactories = new List<IInstallationPluginOptionsFactory>();
            try
            {
                while (true)
                {
                    var installationPluginOptionsFactory = await resolver.GetInstallationPlugin(scope,
                        renewal.StorePluginOptions.Select(x => x.Instance),
                        installationPluginFactories);

                    if (installationPluginOptionsFactory == null)
                    {
                        _exceptionHandler.HandleException(message: $"Installation plugin could not be selected");
                        continue;
                    }
                    InstallationPluginOptions installOptions;
                    try
                    {
                        installOptions = runLevel.HasFlag(RunLevel.Unattended)
                            ? await installationPluginOptionsFactory.Default(target)
                            : await installationPluginOptionsFactory.Aquire(target, _input, runLevel);
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, $"Installation plugin {installationPluginOptionsFactory.Name} aborted or failed");
                        continue;
                    }
                    if (installOptions == null)
                    {
                        _exceptionHandler.HandleException(message: $"Installation plugin {installationPluginOptionsFactory.Name} was unable to generate options");
                        continue;
                    }
                    var isNull = installationPluginOptionsFactory is NullInstallationOptionsFactory;
                    if (!isNull || installationPluginFactories.Count == 0)
                    {
                        ret.Add(installOptions);
                        installationPluginFactories.Add(installationPluginOptionsFactory);
                    }
                    if (isNull)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, "Invalid selection of installation plugins");
                return null;
            }
            return ret;
        }
    }
}
