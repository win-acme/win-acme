using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    internal partial class Wacs
    {
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly IArgumentsService _arguments;
        private readonly IRenewalStore _renewalStore;
        private readonly ISettingsService _settings;
        private readonly ILifetimeScope _container;
        private readonly MainArguments _args;
        private readonly RenewalManager _renewalManager;
        private readonly RenewalCreator _renewalCreator;
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly UserRoleService _userRoleService;
        private readonly TaskSchedulerService _taskScheduler;

        public Wacs(ILifetimeScope container)
        {
            // Basic services
            _container = container;
            _scopeBuilder = container.Resolve<IAutofacBuilder>();
            _exceptionHandler = container.Resolve<ExceptionHandler>();
            _log = _container.Resolve<ILogService>();
            _settings = _container.Resolve<ISettingsService>();
            _userRoleService = _container.Resolve<UserRoleService>();
            _settings = _container.Resolve<ISettingsService>();
            _taskScheduler = _container.Resolve<TaskSchedulerService>();

            try
            {
                Console.OutputEncoding = System.Text.Encoding.GetEncoding(_settings.UI.TextEncoding);
            } 
            catch
            {
                _log.Warning("Error setting text encoding to {name}", _settings.UI.TextEncoding);
            }

            _arguments = _container.Resolve<IArgumentsService>();
            _arguments.ShowCommandLine();
            _args = _arguments.MainArguments;
            _input = _container.Resolve<IInputService>();
            _renewalStore = _container.Resolve<IRenewalStore>();

            var renewalExecutor = container.Resolve<RenewalExecutor>(
                new TypedParameter(typeof(IContainer), _container));
            _renewalManager = container.Resolve<RenewalManager>(
                new TypedParameter(typeof(IContainer), _container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
            _renewalCreator = container.Resolve<RenewalCreator>(
                new TypedParameter(typeof(IContainer), _container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
        }

        /// <summary>
        /// Main program
        /// </summary>
        public async Task<int> Start()
        {
            // Show informational message and start-up diagnostics
            await ShowBanner();

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
                _arguments.ShowHelp();
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
                        await _renewalManager.CancelRenewalsUnattended();
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
                            runLevel |= RunLevel.ForceRenew | RunLevel.IgnoreCache;
                        }
                        await _renewalManager.CheckRenewals(runLevel);
                        await CloseDefault();
                    }
                    else if (!string.IsNullOrEmpty(_args.Target))
                    {
                        await _renewalCreator.SetupRenewal(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.Encrypt)
                    {
                        await Encrypt(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else
                    {
                        await MainMenu();
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
            var build = "";
#if DEBUG
            build += "DEBUG";
#else
            build += "RELEASE";
#endif
#if PLUGGABLE
            build += ", PLUGGABLE";
#else
            build += ", TRIMMED";
#endif
            var version = Assembly.GetEntryAssembly().GetName().Version;
            var iis = _container.Resolve<IIISClient>().Version;
            Console.WriteLine();
            _log.Information(LogType.Screen, "A simple Windows ACMEv2 client (WACS)");
            _log.Information(LogType.Screen, "Software version {version} ({build})", version, build);
            _log.Information(LogType.Disk | LogType.Event, "Software version {version} ({build}) started", version, build);
            if (_args != null)
            {
                _log.Information("ACME server {ACME}", _settings.BaseUri);
                var client = _container.Resolve<AcmeClient>();
                await client.CheckNetwork();
            }
            if (iis.Major > 0)
            {
                _log.Information("IIS version {version}", iis);
            }
            else
            {
                _log.Information("IIS not detected");
            }
            if (_userRoleService.IsAdmin)
            {
                _log.Information("Running with administrator credentials");
            }
            else
            {
                _log.Warning("Running without administrator credentials, some options disabled");
            }
            _taskScheduler.ConfirmTaskScheduler();
            _log.Information("Please report issues at {url}", "https://github.com/PKISharp/win-acme");
            _log.Verbose("Test for international support: {chinese} {russian} {arab}", "語言", "язык", "لغة");
        }

        /// <summary>
        /// Present user with the option to close the program
        /// Useful to keep the console output visible when testing
        /// unattended commands
        /// </summary>
        private async Task CloseDefault()
        {
            _args.CloseOnFinish = 
                _args.Test &&  !_args.CloseOnFinish ? 
                await _input.PromptYesNo("[--test] Quit?", true) : 
                true;
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        private async Task MainMenu()
        {
            var total = _renewalStore.Renewals.Count();
            var due = _renewalStore.Renewals.Count(x => x.IsDue());
            var error = _renewalStore.Renewals.Count(x => !x.History.Last().Success);

            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(RunLevel.Interactive | RunLevel.Simple), 
                    "Create new certificate (simple for IIS)", "N", 
                    @default: _userRoleService.AllowIIS.Item1, 
                    disabled: !_userRoleService.AllowIIS.Item1,
                    disabledReason: _userRoleService.AllowIIS.Item2),
                Choice.Create<Func<Task>>(
                    () => _renewalCreator.SetupRenewal(RunLevel.Interactive | RunLevel.Advanced), 
                    "Create new certificate (full options)", "M", 
                    @default: !_userRoleService.AllowIIS.Item1),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.CheckRenewals(RunLevel.Interactive),
                    $"Run scheduled renewals ({due} currently due)", "R",
                    color: due == 0 ? (ConsoleColor?)null : ConsoleColor.Yellow),
                Choice.Create<Func<Task>>(
                    () => _renewalManager.ManageRenewals(),
                    $"Manage renewals ({total} total{(error == 0 ? "" : $", {error} in error")})", "A",
                    color: error == 0 ? (ConsoleColor?)null : ConsoleColor.Red,
                    disabled: total == 0,
                    disabledReason: "No renewals have been created yet."),
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
                    () => _taskScheduler.EnsureTaskScheduler(RunLevel.Interactive | RunLevel.Advanced, true), 
                    "(Re)create scheduled task", "T", 
                    disabled: !_userRoleService.AllowTaskScheduler,
                    disabledReason: "Run as an administrator to allow access to the task scheduler."),
                Choice.Create<Func<Task>>(
                    () => _container.Resolve<EmailClient>().Test(), 
                    "Test email notification", "E"),
                Choice.Create<Func<Task>>(
                    () => UpdateAccount(RunLevel.Interactive), 
                    "ACME account details", "A"),
                Choice.Create<Func<Task>>(
                    () => Import(RunLevel.Interactive | RunLevel.Advanced), 
                    "Import scheduled renewals from WACS/LEWS 1.9.x", "I", 
                    disabled: !_userRoleService.IsAdmin,
                    disabledReason: "Run as an administrator to allow search for legacy renewals."),
                Choice.Create<Func<Task>>(
                    () => Encrypt(RunLevel.Interactive), 
                    "Encrypt/decrypt configuration", "M"),
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
            var importUri = !string.IsNullOrEmpty(_arguments.MainArguments.ImportBaseUri) ? 
                new Uri(_arguments.MainArguments.ImportBaseUri) : 
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
                _input.Show(null, "To remove machine-dependent protections, use the following steps.", true);
                _input.Show(null, "  1. On your old machine, set the EncryptConfig setting to false");
                _input.Show(null, "  2. Run this option; all protected values will be unprotected.");
                _input.Show(null, "  3. Copy your data files to the new machine.");
                _input.Show(null, "  4. On the new machine, set the EncryptConfig setting to true");
                _input.Show(null, "  5. Run this option; all unprotected values will be saved with protection");
                _input.Show(null, $"Data directory: {settings.Client.ConfigurationPath}", true);
                _input.Show(null, $"Config directory: {new FileInfo(settings.ExePath).Directory.FullName}\\settings.json");
                _input.Show(null, $"Current EncryptConfig setting: {encryptConfig}");
                userApproved = await _input.PromptYesNo($"Save all renewal files {(encryptConfig ? "with" : "without")} encryption?", false);
            }
            if (userApproved)
            {
                _renewalStore.Encrypt(); //re-saves all renewals, forcing re-write of all protected strings decorated with [jsonConverter(typeOf(protectedStringConverter())]

                var acmeClient = _container.Resolve<AcmeClient>();
                acmeClient.EncryptSigner(); //re-writes the signer file

                var certificateService = _container.Resolve<ICertificateService>();
                certificateService.Encrypt(); //re-saves all cached private keys

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
                throw new InvalidOperationException();
            }
            _input.Show("Account ID", acmeAccount.Payload.Id ?? "-", true);
            _input.Show("Created", acmeAccount.Payload.CreatedAt);
            _input.Show("Initial IP", acmeAccount.Payload.InitialIp);
            _input.Show("Status", acmeAccount.Payload.Status);
            if (acmeAccount.Payload.Contact != null &&
                acmeAccount.Payload.Contact.Length > 0)
            {
                _input.Show("Contact(s)", string.Join(", ", acmeAccount.Payload.Contact));
            }
            else
            {
                _input.Show("Contact(s)", "(none)");
            }
            if (await _input.PromptYesNo("Modify contacts?", false))
            {
                await acmeClient.ChangeContacts();
                await UpdateAccount(runLevel);
            }
        }
    }
}