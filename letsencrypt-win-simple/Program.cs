using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
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
            _optionsService = _container.Resolve<IOptionsService>();
            _options = _optionsService.Options;
            if (_options == null) return;
            _input = _container.Resolve<IInputService>();

            // .NET Framework check
            var dn = _container.Resolve<DotNetVersionService>();
            if (!dn.Check())
            {
                return;
            }

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
                    else if (!string.IsNullOrEmpty(_options.Plugin))
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
                    _options.Plugin = null;
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
            return new ScheduledRenewal
            {
                Binding = new Target
                {
                    TargetPluginName = options.Plugin,
                    ValidationPluginName = string.IsNullOrWhiteSpace(options.Validation) ? null : $"{options.ValidationMode}.{options.Validation}"
                },
                New = true,
                Test = options.Test,
                Script = options.Script,
                ScriptParameters = options.ScriptParameters,
                CentralSslStore = options.CentralSslStore,
                CertificateStore = options.CertificateStore,
                KeepExisting = options.KeepExisting,
                InstallationPluginNames = options.Installation.Any() ? options.Installation.ToList() : null,
                Warmup = options.Warmup
            };
        }

        /// <summary>
        /// If renewal is already Scheduled, replace it with the new options
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static ScheduledRenewal CreateRenewal(ScheduledRenewal temp)
        {
            var renewal = _renewalService.Find(temp.Binding);
            if (renewal == null)
            {
                renewal = temp;
            }
            else
            {
                renewal.Updated = true;
            }
            renewal.Test = temp.Test;
            renewal.Binding = temp.Binding;
            renewal.CentralSslStore = temp.CentralSslStore;
            renewal.CertificateStore = temp.CertificateStore;
            renewal.InstallationPluginNames = temp.InstallationPluginNames;
            renewal.KeepExisting = temp.KeepExisting;
            renewal.Script = temp.Script;
            renewal.ScriptParameters = temp.ScriptParameters;
            renewal.Warmup = temp.Warmup;
            return renewal;
        }

        /// <summary>
        /// Remove renewal from the list of scheduled items
        /// </summary>
        private static void CancelRenewal()
        {
            var tempRenewal = CreateRenewal(_options);
            using (var scope = AutofacBuilder.Renewal(_container, tempRenewal, RunLevel.Unattended))
            {
                // Choose target plugin
                var targetPluginFactory = scope.Resolve<ITargetPluginFactory>();
                if (targetPluginFactory is INull)
                {
                    return; // User cancelled or unable to resolve
                }

                // Aquire target
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                var target = targetPlugin.Default(_optionsService);
                if (target == null)
                {
                    _log.Error("Plugin {name} was unable to generate a target", targetPluginFactory.Name);
                    return;
                }

                // Find renewal
                var renewal = _renewalService.Find(target);
                if (renewal == null)
                {
                    _log.Warning("No renewal scheduled for {target}, this run has no effect", target);
                    return;
                }

                // Cancel renewal
                _renewalService.Cancel(renewal);
            }
        }

        /// <summary>
        /// Setup a new scheduled renewal
        /// </summary>
        /// <param name="runLevel"></param>
        private static void CreateNewCertificate(RunLevel runLevel)
        {
            _log.Information(true, "Running in {runLevel} mode", runLevel);
            var tempRenewal = CreateRenewal(_options);
            using (var scope = AutofacBuilder.Renewal(_container, tempRenewal, runLevel))
            {
                // Choose target plugin
                var targetPluginFactory = scope.Resolve<ITargetPluginFactory>();
                if (targetPluginFactory is INull)
                {
                    HandleException(message: $"No target plugin could be selected");
                    return; // User cancelled or unable to resolve
                }

                // Aquire target
                var targetPlugin = scope.Resolve<ITargetPlugin>();
                var target = runLevel == RunLevel.Unattended ? targetPlugin.Default(_optionsService) : targetPlugin.Aquire(_optionsService, _input, runLevel);
                var originalTarget = tempRenewal.Binding;
                tempRenewal.Binding = target;
                if (target == null)
                {
                    HandleException(message: $"Plugin {targetPluginFactory.Name} was unable to generate a target");
                    return;
                }
                tempRenewal.Binding.TargetPluginName = targetPluginFactory.Name;
                tempRenewal.Binding.SSLPort = _options.SSLPort;
                tempRenewal.Binding.SSLIPAddress = _options.SSLIPAddress;
                tempRenewal.Binding.ValidationPort = _options.ValidationPort;
                tempRenewal.Binding.ValidationPluginName = originalTarget.ValidationPluginName;
                _log.Information("Plugin {name} generated target {target}", targetPluginFactory.Name, tempRenewal.Binding);

                // Choose validation plugin
                var validationPluginFactory = scope.Resolve<IValidationPluginFactory>();
                if (validationPluginFactory is INull)
                {
                    HandleException(message: $"No validation plugin could be selected");
                    return; // User cancelled
                }
                else if (!validationPluginFactory.CanValidate(target))
                {
                    // Might happen in unattended mode
                    HandleException(message: $"Validation plugin {validationPluginFactory.Name} is unable to validate target");
                    return;
                }

                // Configure validation
                try
                {
                    if (runLevel == RunLevel.Unattended)
                    {
                        validationPluginFactory.Default(target, _optionsService);
                    }
                    else
                    {
                        validationPluginFactory.Aquire(target, _optionsService, _input, runLevel);
                    }
                    tempRenewal.Binding.ValidationPluginName = $"{validationPluginFactory.ChallengeType}.{validationPluginFactory.Name}";
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid validation input");
                    return;
                }

                // Choose and configure installation plugins
                try
                {
                    var installFactories = scope.Resolve<List<IInstallationPluginFactory>>();
                    if (installFactories.Count == 0)
                    {
                        // User cancelled, otherwise we would at least have the Null-installer
                        return;
                    }
                    foreach (var installFactory in installFactories)
                    {
                        if (runLevel == RunLevel.Unattended)
                        {
                            installFactory.Default(tempRenewal, _optionsService);
                        }
                        else
                        {
                            installFactory.Aquire(tempRenewal, _optionsService, _input, runLevel);
                        }
                    }
                    tempRenewal.InstallationPluginNames = installFactories.Select(f => f.Name).ToList();
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Invalid installation input");
                    return;
                }

                var renewal = CreateRenewal(tempRenewal);
                var result = Renew(scope, renewal);
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
                    _log.Verbose("Checking {renewal}", renewal.Binding.Host);
                    if (renewal.Date >= now)
                    {
                        _log.Information(true, "Renewal for certificate {renewal} is due after {date}", renewal.Binding.Host, renewal.Date.ToUserString());
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
            _log.Information(true, "Renewing certificate for {renewal}", renewal.Binding.Host);
            try
            {
                // Let the plugin run
                var result = Renew(renewal, RunLevel.Unattended);
                _renewalService.Save(renewal, result);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }
        }
    }
}