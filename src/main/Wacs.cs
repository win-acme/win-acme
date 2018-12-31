using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal partial class Wacs
    {
        private IInputService _input;
        private IRenewalService _renewalService;
        private IOptionsService _optionsService;
        private ILogService _log;
        private ILifetimeScope _container;
        private Options _options;
        private AutofacBuilder _scopeBuilder;

        public Wacs(ILifetimeScope container)
        {
            // Basic services
            _container = container;
            _scopeBuilder = container.Resolve<AutofacBuilder>();
            _log = _container.Resolve<ILogService>();
            _optionsService = _container.Resolve<IOptionsService>();
            _options = _optionsService.Options;
            if (_options == null) return;
            _input = _container.Resolve<IInputService>();

            // Show version information
            _input.ShowBanner();

            // Advanced services
            _renewalService = _container.Resolve<IRenewalService>();
        }

        /// <summary>
        /// Main loop
        /// </summary>
        public void Start()
        {
            do
            {
                try
                {
                    if (_options.Import)
                    {
                        Import(RunLevel.Unattended);
                        CloseDefault();
                    }
                    else if (_options.Renew)
                    {
                        CheckRenewals(_options.ForceRenewal);
                        CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_options.Target))
                    {
                        if (_options.Cancel)
                        {
                            CancelRenewal();
                        }
                        else
                        {
                            CreateNewCertificate(RunLevel.Unattended);
                        }
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
                }
                if (!_options.CloseOnFinish)
                {
                    _options.Target = null;
                    _options.Renew = false;
                    _options.ForceRenewal = false;
                    Environment.ExitCode = 0;
                }
            } while (!_options.CloseOnFinish);
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
            if (_options.Test && !_options.CloseOnFinish)
            {
                _options.CloseOnFinish = _input.PromptYesNo("[--test] Quit?");
            }
            else
            {
                _options.CloseOnFinish = true;
            }
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Renewal CreateRenewal(Renewal temp, RunLevel runLevel)
        {
            var renewal = _renewalService.FindByFriendlyName(temp);
            if (renewal == null)
            {
                return temp;
            }
            var overwrite = false;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                overwrite = _input.PromptYesNo($"A renewal with the FriendlyName {temp.FriendlyName} already exists, overwrite?");
            }
            else
            {
                overwrite = true;
            }
            if (overwrite)
            {
                _log.Warning("Overwriting previously created renewal");
                renewal.Updated = true;
                renewal.TargetPluginOptions = temp.TargetPluginOptions;
                renewal.StorePluginOptions = temp.StorePluginOptions;
                renewal.ValidationPluginOptions = temp.ValidationPluginOptions;
                renewal.InstallationPluginOptions = temp.InstallationPluginOptions;
                return renewal;
            }
            else
            {
                return temp;
            }
        }

        /// <summary>
        /// Remove renewal from the list of scheduled items
        /// </summary>
        private void CancelRenewal()
        {
            // TODO: Cancel by friendly name or ID
            //{
            //    // Find renewal
            //    var renewal = _renewalService.Find(target);
            //    if (renewal == null)
            //    {
            //        _log.Warning("No renewal scheduled for {target}, this run has no effect", target);
            //        return;
            //    }

            //    // Cancel renewal
            //    _renewalService.Cancel(renewal);
            //}
        }

        /// <summary>
        /// Setup a new scheduled renewal
        /// </summary>
        /// <param name="runLevel"></param>
        private void CreateNewCertificate(RunLevel runLevel)
        {
            if (_options.Test)
            {
                runLevel |= RunLevel.Test;
            }
            _log.Information(true, "Running in mode: {runLevel}", runLevel);
            using (var configScope = _scopeBuilder.Configuration(_container, runLevel))
            {
                // Choose target plugin
                var tempRenewal = new Renewal();
                var targetPluginOptionsFactory = configScope.Resolve<ITargetPluginOptionsFactory>();
                if (targetPluginOptionsFactory is INull)
                {
                    HandleException(message: $"No target plugin could be selected");
                    return;
                }
                var targetPluginOptions = runLevel.HasFlag(RunLevel.Unattended) ?
                    targetPluginOptionsFactory.Default(_optionsService) :
                    targetPluginOptionsFactory.Aquire(_optionsService, _input, runLevel);
                if (targetPluginOptions == null)
                {
                    HandleException(message: $"Target plugin {targetPluginOptionsFactory.Name} aborted or failed");
                    return;
                }
                tempRenewal.TargetPluginOptions = targetPluginOptions;
                tempRenewal.FriendlyName = targetPluginOptions.FriendlyNameSuggestion;

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
                        validationOptions = validationPluginOptionsFactory.Default(initialTarget, _optionsService);
                    }
                    else
                    {
                        validationOptions = validationPluginOptionsFactory.Aquire(initialTarget, _optionsService, _input, runLevel);
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

                // Choose storage plugin
                var storePluginOptionsFactory = configScope.Resolve<IStorePluginOptionsFactory>();
                if (storePluginOptionsFactory is INull)
                {
                    HandleException(message: $"No store plugin could be selected");
                    return;
                }

                // Configure storage
                try
                {
                    StorePluginOptions storeOptions = null;
                    if (runLevel.HasFlag(RunLevel.Unattended))
                    {
                        storeOptions = storePluginOptionsFactory.Default(_optionsService);
                    }
                    else
                    {
                        storeOptions = storePluginOptionsFactory.Aquire(_optionsService, _input, runLevel);
                    }
                    if (storeOptions == null)
                    {
                        HandleException(message: $"Store plugin {storePluginOptionsFactory.Name} was unable to generate options");
                        return;
                    }
                    tempRenewal.StorePluginOptions = storeOptions;
                }
                catch (Exception ex)
                {
                    HandleException(ex, $"Store plugin {storePluginOptionsFactory.Name} aborted or failed");
                    return;
                }

                // Choose and configure installation plugins
                try
                {
                    var installationPluginOptionsFactories = configScope.Resolve<List<IInstallationPluginOptionsFactory>>();
                    if (installationPluginOptionsFactories.Count() == 0)
                    {
                        // User cancelled, otherwise we would at least have the Null-installer
                        return;
                    }
                    foreach (var installationPluginOptionsFactory in installationPluginOptionsFactories)
                    {
                        InstallationPluginOptions installOptions;
                        try
                        {
                            if (runLevel.HasFlag(RunLevel.Unattended))
                            {
                                installOptions = installationPluginOptionsFactory.Default(initialTarget, _optionsService);
                            }
                            else
                            {
                                installOptions = installationPluginOptionsFactory.Aquire(initialTarget, _optionsService, _input, runLevel);
                            }
                        }
                        catch (Exception ex)
                        {
                            HandleException(ex, $"Install plugin {storePluginOptionsFactory.Name} aborted or failed");
                            return;
                        }
                        if (installOptions == null)
                        {
                            HandleException(message: $"Installation plugin {installationPluginOptionsFactory.Name} was unable to generate options");
                            return;
                        }
                        tempRenewal.InstallationPluginOptions.Add(installOptions);
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
        private void CheckRenewals(bool force)
        {
            _log.Verbose("Checking renewals");
            var renewals = _renewalService.Renewals.ToList();
            if (renewals.Count == 0)
                _log.Warning("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
            {
                if (force)
                {
                    ProcessRenewal(renewal);
                }
                else
                {
                    _log.Verbose("Checking {renewal}", renewal.FriendlyName);
                    if (renewal.Date >= now)
                    {
                        _log.Information(true, "Renewal for {renewal} is due after {date}", renewal.FriendlyName, renewal.Date.ToUserString());
                    }
                    else
                    {
                        ProcessRenewal(renewal);
                    }
                }
            }
        }

        /// <summary>
        /// Process a single renewal
        /// </summary>
        /// <param name="renewal"></param>
        private void ProcessRenewal(Renewal renewal)
        {
            _log.Information(true, "Renewing certificate for {renewal}", renewal.FriendlyName);
            try
            {
                // Let the plugin run
                var result = Renew(renewal, RunLevel.Unattended);
                _renewalService.Save(renewal, result);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.FriendlyName);
            }
        }
    }
}