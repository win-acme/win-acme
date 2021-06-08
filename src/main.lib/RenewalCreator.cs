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
        /// <param name="runLevel"></param>
        internal async Task SetupRenewal(RunLevel runLevel, Renewal? tempRenewal = null)
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
                tempRenewal = Renewal.Create(_args.Id, _settings.ScheduledTask.RenewalDays, _passwordGenerator);
            }
            using var configScope = _scopeBuilder.Configuration(_container, tempRenewal, runLevel);
            // Choose target plugin
            var targetPluginOptionsFactory = configScope.Resolve<ITargetPluginOptionsFactory>();
            if (targetPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No source plugin could be selected");
                return;
            }
            var (targetPluginDisabled, targetPluginDisabledReason) = targetPluginOptionsFactory.Disabled;
            if (targetPluginDisabled)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} is not available. {targetPluginDisabledReason}");
                return;
            }
            var targetPluginOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                await targetPluginOptionsFactory.Default() :
                await targetPluginOptionsFactory.Aquire(_input, runLevel);
            if (targetPluginOptions == null)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} aborted or failed");
                return;
            }
            tempRenewal.TargetPluginOptions = targetPluginOptions;

            // Generate Target and validation plugin choice
            using var targetScope = _scopeBuilder.Target(_container, tempRenewal, runLevel);
            var initialTarget = targetScope.Resolve<Target>();
            if (initialTarget is INull)
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} was unable to generate a target");
                return;
            }
            if (!initialTarget.IsValid(_log))
            {
                _exceptionHandler.HandleException(message: $"Source plugin {targetPluginOptionsFactory.Name} generated an invalid target");
                return;
            }
            _log.Information("Source generated using plugin {name}: {target}", targetPluginOptions.Name, initialTarget);

            // Choose FriendlyName
            if (!string.IsNullOrEmpty(_args.FriendlyName))
            {
                tempRenewal.FriendlyName = _args.FriendlyName;
            }
            else if (runLevel.HasFlag(RunLevel.Advanced | RunLevel.Interactive))
            {
                var alt = await _input.RequestString($"Suggested friendly name '{initialTarget.FriendlyName}', press <Enter> to accept or type an alternative");
                if (!string.IsNullOrEmpty(alt))
                {
                    tempRenewal.FriendlyName = alt;
                }
            }
            tempRenewal.LastFriendlyName = tempRenewal.FriendlyName ?? initialTarget.FriendlyName;

            // Choose validation plugin
            var validationPluginOptionsFactory = targetScope.Resolve<IValidationPluginOptionsFactory>();
            if (validationPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No validation plugin could be selected");
                return;
            }

            // Configure validation
            try
            {
                var validationOptions = runLevel.HasFlag(RunLevel.Unattended)
                    ? await validationPluginOptionsFactory.Default(initialTarget)
                    : await validationPluginOptionsFactory.Aquire(initialTarget, _input, runLevel);
                if (validationOptions == null)
                {
                    _exceptionHandler.HandleException(message: $"Validation plugin {validationPluginOptionsFactory.Name} was unable to generate options");
                    return;
                }
                tempRenewal.ValidationPluginOptions = validationOptions;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Validation plugin {validationPluginOptionsFactory.Name} aborted or failed");
                return;
            }

            // Choose order plugin
            var orderPluginOptionsFactory = targetScope.Resolve<IOrderPluginOptionsFactory>();
            if (orderPluginOptionsFactory is INull)
            {
                _exceptionHandler.HandleException(message: $"No order plugin could be selected");
                return;
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
                    return;
                }
                tempRenewal.OrderPluginOptions = orderOptions;
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex, $"Order plugin {orderPluginOptionsFactory.Name} aborted or failed");
                return;
            }

            // Choose CSR plugin
            if (initialTarget.CsrBytes == null)
            {
                var csrPluginOptionsFactory = configScope.Resolve<ICsrPluginOptionsFactory>();
                if (csrPluginOptionsFactory is INull)
                {
                    _exceptionHandler.HandleException(message: $"No CSR plugin could be selected");
                    return;
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
                        return;
                    }
                    tempRenewal.CsrPluginOptions = csrOptions;
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex, $"CSR plugin {csrPluginOptionsFactory.Name} aborted or failed");
                    return;
                }
            }

            // Choose and configure store plugins
            var resolver = configScope.Resolve<IResolver>();
            var storePluginOptionsFactories = new List<IStorePluginOptionsFactory>();
            try
            {
                while (true)
                {
                    var storePluginOptionsFactory = await resolver.GetStorePlugin(configScope, storePluginOptionsFactories);
                    if (storePluginOptionsFactory == null)
                    {
                        _exceptionHandler.HandleException(message: $"Store could not be selected");
                        return;
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
                        return;
                    }
                    if (storeOptions == null)
                    {
                        _exceptionHandler.HandleException(message: $"Store plugin {storePluginOptionsFactory.Name} was unable to generate options");
                        return;
                    }
                    var isNull = storePluginOptionsFactory is NullStoreOptionsFactory;
                    if (!isNull || storePluginOptionsFactories.Count == 0)
                    {
                        tempRenewal.StorePluginOptions.Add(storeOptions);
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
                return;
            }

            // Choose and configure installation plugins
            var installationPluginFactories = new List<IInstallationPluginOptionsFactory>();
            try
            {
                while (true)
                {
                    var installationPluginOptionsFactory = await resolver.GetInstallationPlugin(configScope,
                        tempRenewal.StorePluginOptions.Select(x => x.Instance),
                        installationPluginFactories);

                    if (installationPluginOptionsFactory == null)
                    {
                        _exceptionHandler.HandleException(message: $"Installation plugin could not be selected");
                        return;
                    }
                    InstallationPluginOptions installOptions;
                    try
                    {
                        installOptions = runLevel.HasFlag(RunLevel.Unattended)
                            ? await installationPluginOptionsFactory.Default(initialTarget)
                            : await installationPluginOptionsFactory.Aquire(initialTarget, _input, runLevel);
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex, $"Installation plugin {installationPluginOptionsFactory.Name} aborted or failed");
                        return;
                    }
                    if (installOptions == null)
                    {
                        _exceptionHandler.HandleException(message: $"Installation plugin {installationPluginOptionsFactory.Name} was unable to generate options");
                        return;
                    }
                    var isNull = installationPluginOptionsFactory is NullInstallationOptionsFactory;
                    if (!isNull || installationPluginFactories.Count == 0)
                    {
                        tempRenewal.InstallationPluginOptions.Add(installOptions);
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
                return;
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

    }
}
