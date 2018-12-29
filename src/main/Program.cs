using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    internal partial class Program
    {
        private static IInputService _input;
        private static IRenewalService _renewalService;
        private static IOptionsService _optionsService;
        private static Options _options;
        private static ILogService _log;
        private static IContainer _container;
        private static ILifetimeScope _legacy;

        private static void Main(string[] args)
        {
            // Setup DI
            _container = AutofacBuilder.Global(args);

            // Basic services
            _log = _container.Resolve<ILogService>();

            // .NET Framework check
            var dn = _container.Resolve<DotNetVersionService>();
            if (!dn.Check())
            {
                return;
            }

            _optionsService = _container.Resolve<IOptionsService>();
            _options = _optionsService.Options;
            if (_options == null) return;
            _input = _container.Resolve<IInputService>();

            // Show version information
            _input.ShowBanner();

            // Advanced services
            _renewalService = _container.Resolve<IRenewalService>();

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Main loop
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
        /// Handle exceptions
        /// </summary>
        /// <param name="ex"></param>
        private static void HandleException(Exception ex = null, string message = null)
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
        private static void CloseDefault()
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
        /// Create new ScheduledRenewal from the options
        /// </summary>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(Options options)
        {
            var ret = new ScheduledRenewal
            {
                New = true,
                Test = options.Test
            };
            return ret;
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(ScheduledRenewal temp)
        {
            var renewal = _renewalService.Find(temp);
            if (renewal == null)
            {
                renewal = temp;
            }
            else
            {
                renewal.Updated = true;
            }
            renewal.Test = temp.Test;
            renewal.FriendlyName = temp.FriendlyName;
            renewal.TargetPluginOptions = temp.TargetPluginOptions;
            renewal.StorePluginOptions = temp.StorePluginOptions;
            renewal.ValidationPluginOptions = temp.ValidationPluginOptions;
            renewal.InstallationPluginOptions = temp.InstallationPluginOptions;
            return renewal;
        }

        /// <summary>
        /// Remove renewal from the list of scheduled items
        /// </summary>
        private static void CancelRenewal()
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
        private static void CreateNewCertificate(RunLevel runLevel)
        {
            _log.Information(true, "Running in {runLevel} mode", runLevel);
            var tempRenewal = CreateRenewal(_options);
            using (var scope = AutofacBuilder.Configuration(_container, tempRenewal, runLevel))
            {
                // Choose target plugin
                var targetPluginOptionsFactory = scope.Resolve<ITargetPluginOptionsFactory>();
                if (targetPluginOptionsFactory is INull)
                {
                    HandleException(message: $"No target plugin could be selected");
                    return;
                }
                var targetPluginOptions = runLevel == RunLevel.Unattended ? 
                    targetPluginOptionsFactory.Default(_optionsService) : 
                    targetPluginOptionsFactory.Aquire(_optionsService, _input, runLevel);
                if (targetPluginOptions == null)
                {
                    HandleException(message: $"Plugin {targetPluginOptionsFactory.Name} was unable to configure");
                    return;
                }
                tempRenewal.TargetPluginOptions = targetPluginOptions;
                tempRenewal.FriendlyName = targetPluginOptions.FriendlyNameSuggestion;

                Target initialTarget = null;
                using (var target = AutofacBuilder.Target(_container, tempRenewal, runLevel))
                {
                    initialTarget = target.Resolve<Target>();
                }
                if (initialTarget == null)
                {
                    HandleException(message: $"Plugin {targetPluginOptionsFactory.Name} was unable to generate a target");
                    return;
                }
                _log.Information("Target generated using plugin {name}: {target}", targetPluginOptions.Name, initialTarget);
   
                // Choose validation plugin
                var validationPluginOptionsFactory = scope.Resolve<IValidationPluginOptionsFactory>();
                if (validationPluginOptionsFactory is INull)
                {
                    HandleException(message: $"No validation plugin could be selected");
                    return;
                }

                // Configure validation
                try
                {
                    ValidationPluginOptions validationOptions = null;
                    if (runLevel == RunLevel.Unattended)
                    {
                        validationOptions = validationPluginOptionsFactory.Default(initialTarget, _optionsService);
                    }
                    else
                    {
                        validationOptions = validationPluginOptionsFactory.Aquire(initialTarget, _optionsService, _input, runLevel);
                    }
                    tempRenewal.ValidationPluginOptions = validationOptions;
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid validation input");
                    return;
                }

                // Choose storage plugin
                var storePluginOptionsFactory = scope.Resolve<IStorePluginOptionsFactory>();
                if (storePluginOptionsFactory is INull)
                {
                    HandleException(message: $"No store plugin could be selected");
                    return;
                }

                // Configure storage
                try
                {
                    StorePluginOptions storeOptions = null;
                    if (runLevel == RunLevel.Unattended)
                    {
                        storeOptions = storePluginOptionsFactory.Default(_optionsService);
                    }
                    else
                    {
                        storeOptions = storePluginOptionsFactory.Aquire(_optionsService, _input, runLevel);
                    }
                    tempRenewal.StorePluginOptions = storeOptions;
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid store input");
                    return;
                }

                // Choose and configure installation plugins
                try
                {
                    var installationPluginOptionsFactories = scope.Resolve<List<IInstallationPluginOptionsFactory>>();
                    if (installationPluginOptionsFactories.Count == 0)
                    {
                        // User cancelled, otherwise we would at least have the Null-installer
                        return;
                    }
                    foreach (var installationPluginOptionsFactory in installationPluginOptionsFactories)
                    {
                        InstallationPluginOptions installOptions;
                        if (runLevel == RunLevel.Unattended)
                        {
                            installOptions = installationPluginOptionsFactory.Default(initialTarget, _optionsService);
                        }
                        else
                        {
                            installOptions = installationPluginOptionsFactory.Aquire(initialTarget, _optionsService, _input, runLevel);
                        }
                        tempRenewal.InstallationPluginOptions.Add(installOptions);
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid installation input");
                    return;
                }

                // Try to run for the first time
                var renewal = CreateRenewal(tempRenewal);
                var result = Renew(scope, renewal, runLevel);
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
        private static void CheckRenewals(bool force)
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
                        _log.Information(true, "Renewal for certificate {renewal} is due after {date}", renewal.FriendlyName, renewal.Date.ToUserString());
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
        private static void ProcessRenewal(ScheduledRenewal renewal)
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