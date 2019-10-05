using Autofac;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Collections.Generic;
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
        private readonly IAutofacBuilder _scopeBuilder;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly UserRoleService _userRoleService;

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

            try
            {
                Console.OutputEncoding = System.Text.Encoding.GetEncoding(_settings.UI.TextEncoding);
            } 
            catch
            {
                _log.Warning("Error setting text encoding to {name}", _settings.UI.TextEncoding);
            }

            ShowBanner();

            _arguments = _container.Resolve<IArgumentsService>();
            _args = _arguments.MainArguments;
            if (_args == null)
            {
                Environment.Exit(1);
            }

            if (_args.Verbose)
            {
                _log.SetVerbose();
                _arguments.ShowCommandLine();
            }
            _input = _container.Resolve<IInputService>();
            _renewalStore = _container.Resolve<IRenewalStore>();

            var renewalExecutor = container.Resolve<RenewalExecutor>(
                new TypedParameter(typeof(IContainer), _container));
            _renewalManager = container.Resolve<RenewalManager>(
                new TypedParameter(typeof(IContainer), _container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
        }

        /// <summary>
        /// Main loop
        /// </summary>
        public async Task Start()
        {
            // Version display (handled by ShowBanner in constructor)
            if (_args.Version)
            {
                await CloseDefault();
                if (_args.CloseOnFinish)
                {
                    return;
                }
            }

            // Help function
            if (_args.Help)
            {
                _arguments.ShowHelp();
                await CloseDefault();
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
                        await Import(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.List)
                    {
                        await _renewalManager.ShowRenewals();
                        await CloseDefault();
                    }
                    else if (_args.Cancel)
                    {
                        await _renewalManager.CancelRenewal(RunLevel.Unattended);
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
                        await _renewalManager.SetupRenewal(RunLevel.Unattended);
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
                    Environment.ExitCode = 0;
                    _container.Resolve<IIISClient>().Refresh();
                }
                else
                {
                    _log.Verbose("Exiting with status code {code}", Environment.ExitCode);
                }
            }
            while (!_args.CloseOnFinish);
        }

        /// <summary>
        /// Show banner
        /// </summary>
        private void ShowBanner()
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
            build += ", UNPLUGGABLE";
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
            var taskScheduler = _container.Resolve<TaskSchedulerService>();
            taskScheduler.ConfirmTaskScheduler();
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
            _args.CloseOnFinish = _args.Test && !_args.CloseOnFinish ? 
                await _input.PromptYesNo("[--test] Quit?", true) : true;
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        private async Task MainMenu()
        {
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(() => _renewalManager.SetupRenewal(RunLevel.Interactive | RunLevel.Simple), "Create new certificate (simple for IIS)", "N", @default: _userRoleService.AllowIIS, disabled: !_userRoleService.AllowIIS),
                Choice.Create<Func<Task>>(() => _renewalManager.SetupRenewal(RunLevel.Interactive | RunLevel.Advanced), "Create new certificate (full options)", "M", @default: !_userRoleService.AllowIIS),
                Choice.Create<Func<Task>>(() => _renewalManager.ShowRenewals(), "List scheduled renewals", "L"),
                Choice.Create<Func<Task>>(() => _renewalManager.CheckRenewals(RunLevel.Interactive), "Renew scheduled", "R"),
                Choice.Create<Func<Task>>(() => _renewalManager.RenewSpecific(), "Renew specific", "S"),
                Choice.Create<Func<Task>>(() => _renewalManager.CheckRenewals(RunLevel.Interactive | RunLevel.ForceRenew), "Renew *all*", "A"),
                Choice.Create<Func<Task>>(() => ExtraMenu(), "More options...", "O"),
                Choice.Create<Func<Task>>(() => { _args.CloseOnFinish = true; _args.Test = false; return Task.CompletedTask; }, "Quit", "Q")
            };
            var chosen = await _input.ChooseFromList("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Less common options
        /// </summary>
        private async Task ExtraMenu()
        {
            var options = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(() => _renewalManager.CancelRenewal(RunLevel.Interactive), "Cancel scheduled renewal", "C"),
                Choice.Create<Func<Task>>(() => _renewalManager.CancelAllRenewals(), "Cancel *all* scheduled renewals", "X"),
                Choice.Create<Func<Task>>(() => RevokeCertificate(), "Revoke certificate", "V"),
                Choice.Create<Func<Task>>(() => _container.Resolve<TaskSchedulerService>().EnsureTaskScheduler(RunLevel.Interactive | RunLevel.Advanced), "(Re)create scheduled task", "T", disabled: !_userRoleService.AllowTaskScheduler),
                Choice.Create<Func<Task>>(() => new Task(() => _container.Resolve<EmailClient>().Test()), "Test email notification", "E"),
                Choice.Create<Func<Task>>(() => UpdateAccount(RunLevel.Interactive), "ACME account details", "A"),
                Choice.Create<Func<Task>>(() => Import(RunLevel.Interactive), "Import scheduled renewals from WACS/LEWS 1.9.x", "I", disabled: !_userRoleService.IsAdmin),
                Choice.Create<Func<Task>>(() => Encrypt(RunLevel.Interactive), "Encrypt/decrypt configuration", "M"),
                Choice.Create<Func<Task>>(() => Task.CompletedTask, "Back", "Q", true)
            };
            var chosen = await _input.ChooseFromList("Please choose from the menu", options);
            await chosen.Invoke();
        }

        /// <summary>
        /// Revoke certificate
        /// </summary>
        private async Task RevokeCertificate()
        {
            var renewal = await _input.ChooseFromList("Which certificate would you like to revoke?",
                _renewalStore.Renewals,
                x => Choice.Create(x),
                "Back");
            if (renewal != null)
            {
                if (await _input.PromptYesNo($"Are you sure you want to revoke {renewal}? This should only be done in case of a security breach.", false))
                {
                    using var scope = _scopeBuilder.Execution(_container, renewal, RunLevel.Unattended);
                    var cs = scope.Resolve<ICertificateService>();
                    try
                    {
                        await cs.RevokeCertificate(renewal);
                        renewal.History.Add(new RenewResult("Certificate revoked"));
                    }
                    catch (Exception ex)
                    {
                        _exceptionHandler.HandleException(ex);
                    }
                }
            }
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
            using var scope = _scopeBuilder.Legacy(_container, importUri, _settings.BaseUri);
            var importer = scope.Resolve<Importer>();
            await importer.Import();
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
                _input.Show(null, $"Data directory: {settings.Client.ConfigPath}", true);
                _input.Show(null, $"Config directory: {Environment.CurrentDirectory}\\settings.json");
                _input.Show(null, $"Current EncryptConfig setting: {encryptConfig}");
                userApproved = await _input.PromptYesNo($"Save all renewal files {(encryptConfig ? "with" : "without")} encryption?", false);
            }
            if (userApproved)
            {
                _log.Information("Updating files in: {settings}", settings.Client.ConfigPath);
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
            _input.Show("Account ID", acmeAccount.Payload.Id, true);
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