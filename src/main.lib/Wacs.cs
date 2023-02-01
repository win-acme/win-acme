using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    internal partial class Wacs
    {
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly IDueDateService _dueDateService;
        private readonly IInputService _input;
        private readonly ISharingLifetimeScope _container;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly IRenewalStore _renewalStore;
        private readonly IUserRoleService _userRoleService;
        private readonly AdminService _adminService;
        private readonly ArgumentsParser _arguments;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly MainArguments _args;
        private readonly RenewalManager _renewalManager;
        private readonly RenewalCreator _renewalCreator;
        private readonly SecretServiceManager _secretServiceManager;
        private readonly ValidationOptionsService _validationOptionsService;
        private readonly TaskSchedulerService _taskScheduler;

        public Wacs(
            ISharingLifetimeScope container, 
            IAutofacBuilder scopeBuilder,
            ExceptionHandler exceptionHandler,
            ILogService logService,
            ISettingsService settingsService,
            IUserRoleService userRoleService,
            IDueDateService dueDateService,
            AdminService adminService,
            RenewalCreator renewalCreator,
            RenewalManager renewalManager,
            TaskSchedulerService taskSchedulerService,
            SecretServiceManager secretServiceManager,
            ValidationOptionsService validationOptionsService)
        {
            // Basic services
            _container = container;
            _scopeBuilder = scopeBuilder;
            _exceptionHandler = exceptionHandler;
            _log = logService;
            _settings = settingsService;
            _adminService = adminService;
            _userRoleService = userRoleService;
            _taskScheduler = taskSchedulerService;
            _secretServiceManager = secretServiceManager;
            _dueDateService = dueDateService;
            _renewalCreator = renewalCreator; 
            _renewalManager = renewalManager;

            if (!string.IsNullOrWhiteSpace(_settings.UI.TextEncoding))
            {
                try
                {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(_settings.UI.TextEncoding);
                }
                catch
                {
                    _log.Warning("Error setting text encoding to {name}", _settings.UI.TextEncoding);
                }
            }

            _arguments = _container.Resolve<ArgumentsParser>();
            _arguments.ShowCommandLine();
            _args = _arguments.GetArguments<MainArguments>()!;
            _input = _container.Resolve<IInputService>();
            _renewalStore = _container.Resolve<IRenewalStore>();
            _validationOptionsService = validationOptionsService;
        }

        /// <summary>
        /// Main program
        /// </summary>
        public async Task<int> Start()
        {
            // Exit when settings are not valid
            if (!_settings.Valid)
            {
                return -1;
            }

            // Show informational message and start-up diagnostics
            await ShowBanner().ConfigureAwait(false);

            // Version display (handled by ShowBanner in constructor)
            if (_args.Version)
            {
                await CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return 0;
                }
            }

            // Help function
            if (_args.Help)
            {
                _arguments.ShowArguments();
                await CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return 0;
                }
            }

            // Main loop
            do
            {
                try
                {
                    if (_args.Import)
                    {
                        await Import(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.List)
                    {
                        await _renewalManager.ShowRenewalsUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Cancel)
                    {
                        _renewalManager.CancelRenewalsUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Revoke)
                    {
                        await _renewalManager.RevokeCertificatesUnattended();
                        await CloseDefault();
                    }
                    else if (_args.Renew)
                    {
                        var runLevel = RunLevel.Unattended;
                        if (_args.Force)
                        {
                            runLevel |= RunLevel.Force;
                        }
                        if (_args.NoCache)
                        {
                            runLevel |= RunLevel.NoCache;
                        }
                        await _renewalManager.CheckRenewals(runLevel);
                        await CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_args.Target) || !string.IsNullOrEmpty(_args.Source))
                    {
                        var runLevel = RunLevel.Unattended;
                        if (_args.Force)
                        {
                            runLevel |= RunLevel.Force | RunLevel.NoCache;
                        }
                        if (_args.NoCache)
                        {
                            runLevel |= RunLevel.NoCache;
                        }
                        await _renewalCreator.SetupRenewal(runLevel);
                        await CloseDefault();
                    }
                    else if (_args.Encrypt)
                    {
                        await Encrypt(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.SetupTaskScheduler)
                    {
                        await _taskScheduler.CreateTaskScheduler(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else
                    {
                        await MainMenu(_args.Test ? RunLevel.Test : RunLevel.None);
                    }
                }
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                    await CloseDefault();
                }
                if (!_args.CloseOnFinish)
                {
                    _args.Clear();
                    _exceptionHandler.ClearError();
                    _container.Resolve<IIISClient>().Refresh();
                }
            }
            while (!_args.CloseOnFinish);

            // Return control to the caller
            _log.Verbose("Exiting with status code {code}", _exceptionHandler.ExitCode);
            return _exceptionHandler.ExitCode;
        }

        /// <summary>
        /// Show banner
        /// </summary>
        private async Task ShowBanner()
        {
            Console.WriteLine();
            _log.Information(LogType.Screen, "A simple Windows ACMEv2 client (WACS)");
            _log.Information(LogType.Screen, "Software version {version} ({build}, {bitness})", VersionService.SoftwareVersion, VersionService.BuildType, VersionService.Bitness);
            _log.Information(LogType.Disk | LogType.Event, "Software version {version} ({build}, {bitness}) started", VersionService.SoftwareVersion, VersionService.BuildType, VersionService.Bitness);
            if (_settings.Client.VersionCheck)
            {
                var client = _container.Resolve<UpdateClient>();
                await client.CheckNewVersion();
            }
            if (_args != null)
            {
                _log.Information("Connecting to {ACME}...", _settings.BaseUri);
                var client = _container.Resolve<AcmeClient>();
                await client.CheckNetwork().ConfigureAwait(false);
            }
            if (_adminService.IsAdmin)
            {
                _log.Debug("Running with administrator credentials");
                var iis = _container.Resolve<IIISClient>().Version;
                if (iis.Major > 0)
                {
                    _log.Debug("IIS version {version}", iis);
                }
                else
                {
                    _log.Debug("IIS not detected");
                }
            }
            else
            {
                _log.Information("Running without administrator credentials, some options disabled");
            }
            _taskScheduler.ConfirmTaskScheduler();
            _log.Information("Please report issues at {url}", "https://github.com/win-acme/win-acme");
            _log.Verbose("Unicode display test: Chinese/{chinese} Russian/{russian} Arab/{arab}", "語言", "язык", "لغة");
        }

        /// <summary>
        /// Present user with the option to close the program
        /// Useful to keep the console output visible when testing
        /// unattended commands
        /// </summary>
        private async Task CloseDefault()
        {
            _args.CloseOnFinish =
                !_args.Test ||
                _args.CloseOnFinish || 
                await _input.PromptYesNo("[--test] Quit?", true);
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        private async Task MainMenu(RunLevel runLevel)
        {
            var total = _renewalStore.Renewals.Count();
            var due = _renewalStore.Renewals.Count(x => _dueDateService.IsDue(x));
            var error = _renewalStore.Renewals.Count(x => !x.History.LastOrDefault()?.Success ?? false);
            var iisState = _userRoleService.IISState;
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(runLevel | RunLevel.Interactive | RunLevel.Simple), 
                    "Create certificate (default settings)", "N", 
                    @default: true),
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(runLevel | RunLevel.Interactive | RunLevel.Advanced),
                    "Create certificate (full options)", "M"),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.CheckRenewals(runLevel | RunLevel.Interactive),
                    $"Run renewals ({due} currently due)", "R",
                    color: due == 0 ? null : ConsoleColor.Yellow,
                    state: total == 0 ? State.DisabledState("No renewals have been created yet.") : State.EnabledState()),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.ManageRenewals(),
                    $"Manage renewals ({total} total{(error == 0 ? "" : $", {error} in error")})", "A",
                    color: error == 0 ? null : ConsoleColor.Red,
                    state: total == 0 ? State.DisabledState("No renewals have been created yet.") : State.EnabledState()),
                Choice.Create<Func<Task>>(
                    () => ExtraMenu(), 
                    "More options...", "O"),
                Choice.Create<Func<Task>>(
                    () => { _args.CloseOnFinish = true; _args.Test = false; return Task.CompletedTask; }, 
                    "Quit", "Q")
            };
            var chosen = await _input.ChooseFromMenu("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Less common options
        /// </summary>
        private async Task ExtraMenu()
        {
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(
                    () => _secretServiceManager.ManageSecrets(),
                    $"Manage secrets", "S"),
                Choice.Create<Func<Task>>(
                    () => _validationOptionsService.Manage(_container),
                    $"Manage global validation options", "V"),
                Choice.Create<Func<Task>>(
                    () => _taskScheduler.CreateTaskScheduler(RunLevel.Interactive | RunLevel.Advanced), 
                    "(Re)create scheduled task", "T",
                    state: !_userRoleService.AllowTaskScheduler ? State.DisabledState("Run as an administrator to allow access to the task scheduler.") : State.EnabledState()),
                Choice.Create<Func<Task>>(
                    () => _container.Resolve<EmailClient>().Test(), 
                    "Test email notification", "E"),
                Choice.Create<Func<Task>>(
                    () => UpdateAccount(RunLevel.Interactive), 
                    "ACME account details", "A"),
                Choice.Create<Func<Task>>(
                    () => Import(RunLevel.Interactive | RunLevel.Advanced), 
                    "Import scheduled renewals from WACS/LEWS 1.9.x", "I",
                    state: !_adminService.IsAdmin ? State.DisabledState("Run as an administrator to allow search for legacy renewals.") : State.EnabledState()),
                Choice.Create<Func<Task>>(
                    () => Encrypt(RunLevel.Interactive), 
                    "Encrypt/decrypt configuration", "M"),
                Choice.Create<Func<Task>>(
                    () => _container.Resolve<UpdateClient>().CheckNewVersion(),
                    "Check for updates", "U"),
                Choice.Create<Func<Task>>(
                    () => Task.CompletedTask, 
                    "Back", "Q",
                    @default: true)
            };
            var chosen = await _input.ChooseFromMenu("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Load renewals from 1.9.x
        /// </summary>
        private async Task Import(RunLevel runLevel)
        {
            var importUri = !string.IsNullOrEmpty(_args.ImportBaseUri) ? 
                new Uri(_args.ImportBaseUri) : 
                _settings.Acme.DefaultBaseUriImport;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                var alt = await _input.RequestString($"Importing renewals for {importUri}, enter to accept or type an alternative");
                if (!string.IsNullOrEmpty(alt))
                {
                    importUri = new Uri(alt);
                }
            }
            if (importUri != null)
            {
                using var scope = _scopeBuilder.Legacy(_container, importUri, _settings.BaseUri);
                var importer = scope.Resolve<Importer>();
                await importer.Import(runLevel);
            }
        }

        /// <summary>
        /// Encrypt/Decrypt all machine-dependent information
        /// </summary>
        private async Task Encrypt(RunLevel runLevel)
        {
            var userApproved = !runLevel.HasFlag(RunLevel.Interactive);
            var encryptConfig = _settings.Security.EncryptConfig;
            var settings = _container.Resolve<ISettingsService>();
            if (!userApproved)
            {
                _input.Show(null, "To move your installation of win-acme to another machine, you will want " +
                "to copy the data directory's files to the new machine. However, if you use the Encrypted Configuration option, your renewal " +
                "files contain protected data that is dependent on your local machine. You can " +
                "use this tools to temporarily unprotect your data before moving from the old machine. " +
                "The renewal files includes passwords for your certificates, other passwords/keys, and a key used " +
                "for signing requests for new certificates.");
                _input.CreateSpace();
                _input.Show(null, "To remove machine-dependent protections, use the following steps.");
                _input.Show(null, "  1. On your old machine, set the EncryptConfig setting to false");
                _input.Show(null, "  2. Run this option; all protected values will be unprotected.");
                _input.Show(null, "  3. Copy your data files to the new machine.");
                _input.Show(null, "  4. On the new machine, set the EncryptConfig setting to true");
                _input.Show(null, "  5. Run this option; all unprotected values will be saved with protection");
                _input.CreateSpace();
                _input.Show(null, $"Data directory: {settings.Client.ConfigurationPath}");
                _input.Show(null, $"Config directory: {new FileInfo(VersionService.ExePath).Directory?.FullName}\\settings.json");
                _input.Show(null, $"Current EncryptConfig setting: {encryptConfig}");
                userApproved = await _input.PromptYesNo($"Save all renewal files {(encryptConfig ? "with" : "without")} encryption?", false);
            }
            if (userApproved)
            {
                _renewalStore.Encrypt(); //re-saves all renewals, forcing re-write of all protected strings 

                var accountManager = _container.Resolve<AccountManager>();
                accountManager.Encrypt(); //re-writes the signer file

                var cacheService = _container.Resolve<ICacheService>();
                cacheService.Encrypt(); //re-saves all cached private keys

                var secretService = _container.Resolve<SecretServiceManager>();
                secretService.Encrypt(); //re-writes the secrets file

                var orderManager = _container.Resolve<OrderManager>();
                orderManager.Encrypt(); //re-writes the cached order files

                var validationOptionsService = _container.Resolve<IValidationOptionsService>();
                await validationOptionsService.Encrypt(); //re-saves all global validation options

                _log.Information("Your files are re-saved with encryption turned {onoff}", encryptConfig ? "on" : "off");
            }
        }

        /// <summary>
        /// Check/update account information
        /// </summary>
        /// <param name="runLevel"></param>
        private async Task UpdateAccount(RunLevel runLevel)
        {
            var acmeClient = _container.Resolve<AcmeClient>();
            var acmeAccount = await acmeClient.GetAccount();
            if (acmeAccount == null)
            {
                throw new InvalidOperationException("Unable to initialize acmeAccount");
            }
            _input.CreateSpace();
            _input.Show("Account ID", acmeAccount.Value.Payload.Id ?? "-");
            _input.Show("Account KID", acmeAccount.Value.Kid ?? "-");
            _input.Show("Created", acmeAccount.Value.Payload.CreatedAt);
            _input.Show("Initial IP", acmeAccount.Value.Payload.InitialIp);
            _input.Show("Status", acmeAccount.Value.Payload.Status);
            if (acmeAccount.Value.Payload.Contact != null &&
                acmeAccount.Value.Payload.Contact.Length > 0)
            {
                _input.Show("Contact(s)", string.Join(", ", acmeAccount.Value.Payload.Contact));
            }
            else
            {
                _input.Show("Contact(s)", "(none)");
            }
            if (await _input.PromptYesNo("Modify contacts?", false))
            {
                try
                {
                    await acmeClient.ChangeContacts();
                    await UpdateAccount(runLevel);
                } 
                catch (Exception ex)
                {
                    _exceptionHandler.HandleException(ex);
                }
            }
        }
    }
}