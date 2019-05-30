using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
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
using System.Reflection;

namespace PKISharp.WACS
{
    internal partial class Wacs
    {
        private IInputService _input;
        private IRenewalService _renewalService;
        private IArgumentsService _arguments;
        private ILogService _log;
        private ILifetimeScope _container;
        private MainArguments _args;
        private EmailClient _email;
        private AutofacBuilder _scopeBuilder;
        private PasswordGenerator _passwordGenerator;

        public Wacs(ILifetimeScope container)
        {
            // Basic services
            _container = container;
            _scopeBuilder = container.Resolve<AutofacBuilder>();
            _passwordGenerator = container.Resolve<PasswordGenerator>();
            _log = _container.Resolve<ILogService>();

            ShowBanner();

            _arguments = _container.Resolve<IArgumentsService>();
            _args = _arguments.MainArguments;
            if (_args != null)
            {
                if (_args.Verbose)
                {
                    _log.SetVerbose();
                    _arguments.ShowCommandLine();
                }
                _email = container.Resolve<EmailClient>();
                _input = _container.Resolve<IInputService>();
                _renewalService = _container.Resolve<IRenewalService>();
            }
            else
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Main loop
        /// </summary>
        public void Start()
        {
            // Version display (handled by ShowBanner in constructor)
            if (_args.Version)
            {
                CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return;
                }
            }

            // Help function
            if (_args.Help)
            {
                _arguments.ShowHelp();
                CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return;
                }
            }

            // Main loop
            do
            {
                try
                {
                    if (_args.Import)
                    {
                        Import(RunLevel.Unattended);
                        CloseDefault();
                    }
                    else if (_args.List)
                    {
                        ShowRenewals();
                        CloseDefault();
                    }
                    else if (_args.Cancel)
                    {
                        CancelRenewal(RunLevel.Unattended);
                        CloseDefault();
                    }
                    else if (_args.Renew)
                    {
                        var runLevel = RunLevel.Unattended;
                        if (_args.Force)
                        {
                            runLevel |= (RunLevel.ForceRenew | RunLevel.IgnoreCache);
                        }
                        CheckRenewals(runLevel);
                        CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_args.Target))
                    {
                        CreateNewCertificate(RunLevel.Unattended);
                        CloseDefault();
                    }
                    else
                    {
                        MainMenu();
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                    CloseDefault();
                }
                if (!_args.CloseOnFinish)
                {
                    _args.Clear();
                    Environment.ExitCode = 0;
                }
            } while (!_args.CloseOnFinish);
        }

        /// <summary>
        /// Show banner
        /// </summary>
        private void ShowBanner()
        {
#if DEBUG
            var build = "DEBUG";
#else
            var build = "RELEASE";
#endif
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var iis = _container.Resolve<IIISClient>().Version;
            Console.WriteLine();
            _log.Information(true, "A simple Windows ACMEv2 client (WACS)");
            _log.Information(true, "Software version {version} ({build})", version, build);
            if (_args != null)
            {
                _log.Information("ACME server {ACME}", _args.GetBaseUri());
            }
            if (iis.Major > 0)
            {
                _log.Information("IIS version {version}", iis);
            }
            else
            {
                _log.Information("IIS not detected");
            }
            _log.Information("Please report issues at {url}", "https://github.com/PKISharp/win-acme");
            Console.WriteLine();
        }

        /// <summary>
        /// Handle exceptions by logging them and setting negative exit code
        /// </summary>
        /// <param name="ex"></param>
        private void HandleException(Exception ex = null, string message = null)
        {
            if (ex != null)
            {
                _log.Debug($"{ex.GetType().Name}: {{@e}}", ex);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    _log.Debug($"Inner: {ex.GetType().Name}: {{@e}}", ex);
                }
                _log.Error($"{ex.GetType().Name}: {{e}}", string.IsNullOrEmpty(message) ? ex.Message : message);
                Environment.ExitCode = ex.HResult;
            }
            else if (!string.IsNullOrEmpty(message))
            {
                _log.Error(message);
                Environment.ExitCode = -1;
            }
        }

        /// <summary>
        /// Present user with the option to close the program
        /// Useful to keep the console output visible when testing
        /// unattended commands
        /// </summary>
        private void CloseDefault()
        {
            if (_args.Test && !_args.CloseOnFinish)
            {
                _args.CloseOnFinish = _input.PromptYesNo("[--test] Quit?", true);
            }
            else
            {
                _args.CloseOnFinish = true;
            }
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Renewal CreateRenewal(Renewal temp, RunLevel runLevel)
        {
            // First check by id
            var existing = _renewalService.FindByArguments(temp.Id, null).FirstOrDefault();

            // If Id has been specified, we don't consider the Friendlyname anymore
            // So specifying --id becomes a way to create duplicate certificates
            // with the same --friendlyname in unattended mode.
            if (existing == null && string.IsNullOrEmpty(_args.Id))
            {
                existing = _renewalService.FindByArguments(null, temp.LastFriendlyName).FirstOrDefault();
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
                _input.Show("Existing renewal", existing.ToString(), true);
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
        private void CancelRenewal(RunLevel runLevel)
        {
            if (runLevel.HasFlag(RunLevel.Unattended))
            {
                if (!_arguments.HasFilter())
                {
                    _log.Error("Specify which renewal to cancel using the parameter --id or --friendlyname.");
                    return;
                }
                var targets = _renewalService.FindByArguments(
                    _arguments.MainArguments.Id,
                    _arguments.MainArguments.FriendlyName);
                if (targets.Count() == 0)
                {
                    _log.Error("No renewals matched.");
                    return;
                }
                foreach (var r in targets)
                {
                    _renewalService.Cancel(r);
                }
            }  
            else
            {
                var renewal = _input.ChooseFromList("Which renewal would you like to cancel?",
                    _renewalService.Renewals,
                    x => Choice.Create(x),
                    "Back");
                if (renewal != null)
                {
                    if (_input.PromptYesNo($"Are you sure you want to cancel the renewal for {renewal}", false))
                    {
                        _renewalService.Cancel(renewal);
                    }
                }
            }
        }

        /// <summary>
        /// Setup a new scheduled renewal
        /// </summary>
        /// <param name="runLevel"></param>
        private void CreateNewCertificate(RunLevel runLevel)
        {
            if (_args.Test)
            {
                runLevel |= RunLevel.Test;
            }
            _log.Information(true, "Running in mode: {runLevel}", runLevel);
            var tempRenewal = Renewal.Create(_args.Id, _passwordGenerator);
            using (var configScope = _scopeBuilder.Configuration(_container, tempRenewal, runLevel))
            {
                // Choose target plugin
                var targetPluginOptionsFactory = configScope.Resolve<ITargetPluginOptionsFactory>();
                if (targetPluginOptionsFactory is INull)
                {
                    HandleException(message: $"No target plugin could be selected");
                    return;
                }
                var targetPluginOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                    targetPluginOptionsFactory.Default(_arguments) :
                    targetPluginOptionsFactory.Aquire(_arguments, _input, runLevel);
                if (targetPluginOptions == null)
                {
                    HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} aborted or failed");
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
                        HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} was unable to generate a target");
                        return;
                    }
                    if (!initialTarget.IsValid(_log))
                    {
                        HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} generated an invalid target");
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
                        HandleException(message: $"No validation plugin could be selected");
                        return;
                    }
                }

                // Configure validation
                try
                {
                    ValidationPluginOptions validationOptions = null;
                    if (runLevel.HasFlag(RunLevel.Unattended))
                    {
                        validationOptions = validationPluginOptionsFactory.Default(initialTarget, _arguments);
                    }
                    else
                    {
                        validationOptions = validationPluginOptionsFactory.Aquire(initialTarget, _arguments, _input, runLevel);
                    }
                    if (validationOptions == null)
                    {
                        HandleException(message: $"Validation plugin {validationPluginOptionsFactory.Name} was unable to generate options");
                        return;
                    }
                    tempRenewal.ValidationPluginOptions = validationOptions;
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"Validation plugin {validationPluginOptionsFactory.Name} aborted or failed");
                    return;
                }

                // Choose CSR plugin
                var csrPluginOptionsFactory = configScope.Resolve<ICsrPluginOptionsFactory>();
                if (csrPluginOptionsFactory is INull)
                {
                    HandleException(message: $"No CSR plugin could be selected");
                    return;
                }

                // Configure CSR
                try
                {
                    CsrPluginOptions csrOptions = null;
                    if (runLevel.HasFlag(RunLevel.Unattended))
                    {
                        csrOptions =csrPluginOptionsFactory.Default(_arguments);
                    }
                    else
                    {
                        csrOptions = csrPluginOptionsFactory.Aquire(_arguments, _input, runLevel);
                    }
                    if (csrOptions == null)
                    {
                        HandleException(message: $"CSR plugin {csrPluginOptionsFactory.Name} was unable to generate options");
                        return;
                    }
                    tempRenewal.CsrPluginOptions = csrOptions;
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"CSR plugin {csrPluginOptionsFactory.Name} aborted or failed");
                    return;
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
                            HandleException(message: $"Store could not be selected");
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
                                storeOptions = storePluginOptionsFactory.Default(_arguments);
                            }
                            else
                            {
                                storeOptions = storePluginOptionsFactory.Aquire(_arguments, _input, runLevel);
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, $"Store plugin {storePluginOptionsFactory.Name} aborted or failed");
                            return;
                        }
                        if (storeOptions == null)
                        {
                            HandleException(message: $"Store plugin {storePluginOptionsFactory.Name} was unable to generate options");
                            return;
                        }
                        tempRenewal.StorePluginOptions.Add(storeOptions);
                        storePluginOptionsFactories.Add(storePluginOptionsFactory);
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid selection of store plugins");
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
                            HandleException(message: $"Installation plugin could not be selected");
                        }
                        if (installationPluginFactory is NullInstallationOptionsFactory)
                        {
                            if (installationPluginFactories.Count == 0)
                            {
                                installationPluginFactories.Add(installationPluginFactory);
                            }
                        }
                        InstallationPluginOptions installOptions;
                        try
                        {
                            if (runLevel.HasFlag(RunLevel.Unattended))
                            {
                                installOptions = installationPluginFactory.Default(initialTarget, _arguments);
                            }
                            else
                            {
                                installOptions = installationPluginFactory.Aquire(initialTarget, _arguments, _input, runLevel);
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, $"Installation plugin {installationPluginFactory.Name} aborted or failed");
                            return;
                        }
                        if (installOptions == null)
                        {
                            HandleException(message: $"Installation plugin {installationPluginFactory.Name} was unable to generate options");
                            return;
                        }
                        tempRenewal.InstallationPluginOptions.Add(installOptions);
                        installationPluginFactories.Add(installationPluginFactory);
                        if (installationPluginFactory is NullInstallationOptionsFactory)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid selection of installation plugins");
                    return;
                }

                // Try to run for the first time
                var renewal = CreateRenewal(tempRenewal, runLevel);
                var result = Renew(renewal, runLevel);
                if (!result.Success)
                {
                    HandleException(message: $"Create certificate failed: {result.ErrorMessage}");
                }
                else
                {
                    _renewalService.Save(renewal, result);
                }
            }
        }

        /// <summary>
        /// Loop through the store renewals and run those which are
        /// due to be run
        /// </summary>
        private void CheckRenewals(RunLevel runLevel)
        {
            IEnumerable<Renewal> renewals = null;
            if (_arguments.HasFilter())
            {
                renewals = _renewalService.FindByArguments(_args.Id, _args.FriendlyName);
                if (renewals.Count() == 0)
                {
                    _log.Error("No renewals found that match the filter parameters --id and/or --friendlyname.");
                }
            }
            else
            {
                _log.Verbose("Checking renewals");
                renewals = _renewalService.Renewals;
                if (renewals.Count() == 0)
                {
                    _log.Warning("No scheduled renewals found.");
                }
            }

            if (renewals.Count() > 0)
            {
                WarnAboutRenewalArguments();
                var now = DateTime.UtcNow;
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
        private void ProcessRenewal(Renewal renewal, RunLevel runLevel)
        {
            var notification = _container.Resolve<NotificationService>();
            try
            {
                var result = Renew(renewal, runLevel);
                if (result != null)
                {
                    _renewalService.Save(renewal, result);
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
                HandleException(ex);
                notification.NotifyFailure(runLevel, renewal, ex.Message);
            }
        }

        private void WarnAboutRenewalArguments()
        {
            if (_arguments.Active())
            {
                _log.Warning("You have specified command line options for plugins. " +
                    "Note that these only affect new certificates, but NOT existing renewals. " +
                    "To change settings, re-create (overwrite) the renewal.");
            }
        }
    }
}