using Autofac;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    class RenewalManager
    {
        private readonly IInputService _input;
        private readonly ILogService _log;
        private readonly IRenewalStore _renewalStore;
        private readonly IArgumentsService _arguments;
        private readonly MainArguments _args;
        private readonly PasswordGenerator _passwordGenerator;
        private readonly ISettingsService _settings;
        private readonly IContainer _container;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly RenewalExecutor _renewalExecution;

        public RenewalManager(
            IArgumentsService arguments, PasswordGenerator passwordGenerator,
            MainArguments args, IRenewalStore renewalStore, IContainer container,
            IInputService input, ILogService log, ISettingsService settings,
            IAutofacBuilder autofacBuilder, ExceptionHandler exceptionHandler,
            RenewalExecutor renewalExecutor)
        {
            _passwordGenerator = passwordGenerator;
            _renewalStore = renewalStore;
            _args = args;
            _input = input;
            _log = log;
            _arguments = arguments;
            _settings = settings;
            _container = container;
            _scopeBuilder = autofacBuilder;
            _exceptionHandler = exceptionHandler;
            _renewalExecution = renewalExecutor;
        } 

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Renewal CreateRenewal(Renewal temp, RunLevel runLevel)
        {
            // First check by id
            var existing = _renewalStore.FindByArguments(temp.Id, null).FirstOrDefault();

            // If Id has been specified, we don't consider the Friendlyname anymore
            // So specifying --id becomes a way to create duplicate certificates
            // with the same --friendlyname in unattended mode.
            if (existing == null && string.IsNullOrEmpty(_args.Id))
            {
                existing = _renewalStore.FindByArguments(null, temp.LastFriendlyName).FirstOrDefault();
            }

            // This will be a completely new renewal, no further processing needed
            if (existing == null)
            {
                return temp;
            }

            // Match found with existing certificate, determine if we want to overwrite
            // it or create it side by side with the current one.
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                _input.Show("Existing renewal", existing.ToString(_input), true);
                if (!_input.PromptYesNo($"Overwrite?", true))
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
        /// Remove renewal from the list of scheduled items
        /// </summary>
        internal void CancelRenewal(RunLevel runLevel)
        {
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                if (!_arguments.HasFilter())
                {
                    _log.Error("Specify which renewal to cancel using the parameter --id or --friendlyname.");
                    return;
                }
                var targets = _renewalStore.FindByArguments(
                    _arguments.MainArguments.Id,
                    _arguments.MainArguments.FriendlyName);
                if (targets.Count() == 0)
                {
                    _log.Error("No renewals matched.");
                    return;
                }
                foreach (var r in targets)
                {
                    _renewalStore.Cancel(r);
                }
            }
            else
            {
                var renewal = _input.ChooseFromList("Which renewal would you like to cancel?",
                    _renewalStore.Renewals,
                    x => Choice.Create(x),
                    "Back");
                if (renewal != null)
                {
                    if (_input.PromptYesNo($"Are you sure you want to cancel the renewal for {renewal}", false))
                    {
                        _renewalStore.Cancel(renewal);
                    }
                }
            }
        }

        /// <summary>
        /// Cancel all renewals
        /// </summary>
        internal void CancelAllRenewals()
        {
            var renewals = _renewalStore.Renewals;
            _input.WritePagedList(renewals.Select(x => Choice.Create(x)));
            if (_input.PromptYesNo("Are you sure you want to cancel all of these?", false))
            {
                _renewalStore.Clear();
            }
        }

        /// <summary>
        /// Setup a new scheduled renewal
        /// </summary>
        /// <param name="runLevel"></param>
        internal void SetupRenewal(RunLevel runLevel)
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
            var tempRenewal = Renewal.Create(_args.Id, _settings.RenewalDays, _passwordGenerator);
            using (var configScope = _scopeBuilder.Configuration(_container, tempRenewal, runLevel))
            {
                // Choose target plugin
                var targetPluginOptionsFactory = configScope.Resolve<ITargetPluginOptionsFactory>();
                if (targetPluginOptionsFactory is INull)
                {
                    _exceptionHandler.HandleException(message: $"No target plugin could be selected");
                    return;
                }
                var targetPluginOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                    targetPluginOptionsFactory.Default() :
                    targetPluginOptionsFactory.Aquire(_input, runLevel);
                if (targetPluginOptions == null)
                {
                    _exceptionHandler.HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} aborted or failed");
                    return;
                }
                tempRenewal.TargetPluginOptions = targetPluginOptions;

                // Generate Target and validation plugin choice
                Target initialTarget = null;
                IValidationPluginOptionsFactory validationPluginOptionsFactory = null;
                using (var targetScope = _scopeBuilder.Target(_container, tempRenewal, runLevel))
                {
                    initialTarget = targetScope.Resolve<Target>();
                    if (initialTarget == null)
                    {
                        _exceptionHandler.HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} was unable to generate a target");
                        return;
                    }
                    if (!initialTarget.IsValid(_log))
                    {
                        _exceptionHandler.HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} generated an invalid target");
                        return;
                    }
                    _log.Information("Target generated using plugin {name}: {target}", targetPluginOptions.Name, initialTarget);

                    // Choose FriendlyName
                    if (runLevel.HasFlag(RunLevel.Advanced) &&
                        runLevel.HasFlag(RunLevel.Interactive) &&
                        string.IsNullOrEmpty(_args.FriendlyName))
                    {
                        var alt = _input.RequestString($"Suggested FriendlyName is '{initialTarget.FriendlyName}', press enter to accept or type an alternative");
                        if (!string.IsNullOrEmpty(alt))
                        {
                            tempRenewal.FriendlyName = alt;
                        }
                    }
                    tempRenewal.LastFriendlyName = initialTarget.FriendlyName;

                    // Choose validation plugin
                    validationPluginOptionsFactory = targetScope.Resolve<IValidationPluginOptionsFactory>();
                    if (validationPluginOptionsFactory is INull)
                    {
                        _exceptionHandler.HandleException(message: $"No validation plugin could be selected");
                        return;
                    }
                }

                // Configure validation
                try
                {
                    ValidationPluginOptions validationOptions = null;
                    if (runLevel.HasFlag(RunLevel.Unattended))
                    {
                        validationOptions = validationPluginOptionsFactory.Default(initialTarget);
                    }
                    else
                    {
                        validationOptions = validationPluginOptionsFactory.Aquire(initialTarget, _input, runLevel);
                    }
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
                        CsrPluginOptions csrOptions = null;
                        if (runLevel.HasFlag(RunLevel.Unattended))
                        {
                            csrOptions = csrPluginOptionsFactory.Default();
                        }
                        else
                        {
                            csrOptions = csrPluginOptionsFactory.Aquire(_input, runLevel);
                        }
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
                        var storePluginOptionsFactory = resolver.GetStorePlugin(configScope, storePluginOptionsFactories);
                        if (storePluginOptionsFactory == null)
                        {
                            _exceptionHandler.HandleException(message: $"Store could not be selected");
                        }
                        if (storePluginOptionsFactory is NullStoreOptionsFactory)
                        {
                            if (storePluginOptionsFactories.Count == 0)
                            {
                                throw new Exception();
                            }
                            break;
                        }
                        StorePluginOptions storeOptions;
                        try
                        {
                            if (runLevel.HasFlag(RunLevel.Unattended))
                            {
                                storeOptions = storePluginOptionsFactory.Default();
                            }
                            else
                            {
                                storeOptions = storePluginOptionsFactory.Aquire(_input, runLevel);
                            }
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
                        tempRenewal.StorePluginOptions.Add(storeOptions);
                        storePluginOptionsFactories.Add(storePluginOptionsFactory);
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
                        var installationPluginFactory = resolver.GetInstallationPlugin(configScope,
                            tempRenewal.StorePluginOptions.Select(x => x.Instance),
                            installationPluginFactories);

                        if (installationPluginFactory == null)
                        {
                            _exceptionHandler.HandleException(message: $"Installation plugin could not be selected");
                        }
                        InstallationPluginOptions installOptions;
                        try
                        {
                            if (runLevel.HasFlag(RunLevel.Unattended))
                            {
                                installOptions = installationPluginFactory.Default(initialTarget);
                            }
                            else
                            {
                                installOptions = installationPluginFactory.Aquire(initialTarget, _input, runLevel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _exceptionHandler.HandleException(ex, $"Installation plugin {installationPluginFactory.Name} aborted or failed");
                            return;
                        }
                        if (installOptions == null)
                        {
                            _exceptionHandler.HandleException(message: $"Installation plugin {installationPluginFactory.Name} was unable to generate options");
                            return;
                        }
                        if (installationPluginFactory is NullInstallationOptionsFactory)
                        {
                            if (installationPluginFactories.Count == 0)
                            {
                                tempRenewal.InstallationPluginOptions.Add(installOptions);
                                installationPluginFactories.Add(installationPluginFactory);
                            }
                            break;
                        }
                        tempRenewal.InstallationPluginOptions.Add(installOptions);
                        installationPluginFactories.Add(installationPluginFactory);
                    }
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex, "Invalid selection of installation plugins");
                    return;
                }

                // Try to run for the first time
                var renewal = CreateRenewal(tempRenewal, runLevel);
                var result = _renewalExecution.Renew(renewal, runLevel);
                if (!result.Success)
                {
                    _exceptionHandler.HandleException(message: $"Create certificate failed: {result.ErrorMessage}");
                }
                else
                {
                    _renewalStore.Save(renewal, result);
                }
            }
        }

        /// <summary>
        /// Loop through the store renewals and run those which are
        /// due to be run
        /// </summary>
        internal void CheckRenewals(RunLevel runLevel)
        {
            IEnumerable<Renewal> renewals;
            if (_arguments.HasFilter())
            {
                renewals = _renewalStore.FindByArguments(_args.Id, _args.FriendlyName);
                if (renewals.Count() == 0)
                {
                    _log.Error("No renewals found that match the filter parameters --id and/or --friendlyname.");
                }
            }
            else
            {
                _log.Verbose("Checking renewals");
                renewals = _renewalStore.Renewals;
                if (renewals.Count() == 0)
                {
                    _log.Warning("No scheduled renewals found.");
                }
            }

            if (renewals.Count() > 0)
            {
                WarnAboutRenewalArguments();
                foreach (var renewal in renewals)
                {
                    ProcessRenewal(renewal, runLevel);
                }
            }
        }

        /// <summary>
        /// Process a single renewal
        /// </summary>
        /// <param name="renewal"></param>
        internal void ProcessRenewal(Renewal renewal, RunLevel runLevel)
        {
            var notification = _container.Resolve<NotificationService>();
            try
            {
                var result = _renewalExecution.Renew(renewal, runLevel);
                if (result != null)
                {
                    _renewalStore.Save(renewal, result);
                    if (result.Success)
                    {
                        notification.NotifySuccess(runLevel, renewal);
                    }
                    else
                    {
                        notification.NotifyFailure(runLevel, renewal, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.HandleException(ex);
                notification.NotifyFailure(runLevel, renewal, ex.Message);
            }
        }

        internal void WarnAboutRenewalArguments()
        {
            if (_arguments.Active())
            {
                _log.Warning("You have specified command line options for plugins. " +
                    "Note that these only affect new certificates, but NOT existing renewals. " +
                    "To change settings, re-create (overwrite) the renewal.");
            }
        }


        /// <summary>
        /// Show certificate details
        /// </summary>
        internal void ShowRenewals()
        {
            var renewal = _input.ChooseFromList("Type the number of a renewal to show its details, or press enter to go back",
                _renewalStore.Renewals,
                x => Choice.Create(x,
                    description: x.ToString(_input),
                    color: x.History.Last().Success ?
                            x.IsDue() ?
                                ConsoleColor.DarkYellow :
                                ConsoleColor.Green :
                            ConsoleColor.Red),
                "Back");

            if (renewal != null)
            {
                try
                {
                    _input.Show("Renewal");
                    _input.Show("Id", renewal.Id);
                    _input.Show("File", $"{renewal.Id}.renewal.json");
                    _input.Show("FriendlyName", string.IsNullOrEmpty(renewal.FriendlyName) ? $"[Auto] {renewal.LastFriendlyName}" : renewal.FriendlyName);
                    _input.Show(".pfx password", renewal.PfxPassword?.Value);
                    _input.Show("Renewal due", renewal.GetDueDate()?.ToString() ?? "now");
                    _input.Show("Renewed", $"{renewal.History.Where(x => x.Success).Count()} times");
                    renewal.TargetPluginOptions.Show(_input);
                    renewal.ValidationPluginOptions.Show(_input);
                    renewal.CsrPluginOptions.Show(_input);
                    foreach (var ipo in renewal.StorePluginOptions)
                    {
                        ipo.Show(_input);
                    }
                    foreach (var ipo in renewal.InstallationPluginOptions)
                    {
                        ipo.Show(_input);
                    }
                    _input.Show("History");
                    _input.WritePagedList(renewal.History.Select(x => Choice.Create(x)));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to list details for target");
                }
            }
        }

        /// <summary>
        /// Renew specific certificate
        /// </summary>
        internal void RenewSpecific()
        {
            var renewal = _input.ChooseFromList("Which renewal would you like to run?",
                _renewalStore.Renewals,
                x => Choice.Create(x),
                "Back");
            if (renewal != null)
            {
                var runLevel = RunLevel.Interactive | RunLevel.ForceRenew;
                if (_args.Force)
                {
                    runLevel |= RunLevel.IgnoreCache;
                }
                WarnAboutRenewalArguments();
                ProcessRenewal(renewal, runLevel);
            }
        }
    }
}