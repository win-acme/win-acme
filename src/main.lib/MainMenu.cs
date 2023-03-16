using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
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
    internal class MainMenu
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
        private readonly AcmeClient _acmeClient;

        public MainMenu(
            ISharingLifetimeScope container, 
            IAutofacBuilder scopeBuilder,
            ExceptionHandler exceptionHandler,
            ILogService logService,
            IInputService inputService,
            ISettingsService settingsService,
            IUserRoleService userRoleService,
            IDueDateService dueDateService,
            IRenewalStore renewalStore,
            ArgumentsParser argumentsParser,
            AdminService adminService,
            RenewalCreator renewalCreator,
            RenewalManager renewalManager,
            TaskSchedulerService taskSchedulerService,
            SecretServiceManager secretServiceManager,
            AcmeClient acmeClient,
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
            _arguments = argumentsParser;
            _input = inputService;
            _renewalStore = renewalStore;
            _acmeClient = acmeClient;
            _validationOptionsService = validationOptionsService;
            _args = _arguments.GetArguments<MainArguments>() ?? new MainArguments();
        }

        /// <summary>
        /// Main user experience
        /// </summary>
        public async Task MainMenuEntry(RunLevel runLevel)
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
        internal async Task Import(RunLevel runLevel)
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
        internal async Task Encrypt(RunLevel runLevel)
        {
            var userApproved = !runLevel.HasFlag(RunLevel.Interactive);
            var encryptConfig = _settings.Security.EncryptConfig;
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
                _input.Show(null, $"Data directory: {_settings.Client.ConfigurationPath}");
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
            var client = await _acmeClient.GetClient();
            if (client == null)
            {
                throw new InvalidOperationException("Unable to initialize acmeAccount");
            }
            var accountDetails = client.Account.Details;
            _input.CreateSpace();
            _input.Show("Account ID", accountDetails.Payload.Id ?? "-");
            _input.Show("Account KID", accountDetails.Kid ?? "-");
            _input.Show("Created", accountDetails.Payload.CreatedAt);
            _input.Show("Initial IP", accountDetails.Payload.InitialIp);
            _input.Show("Status", accountDetails.Payload.Status);
            if (accountDetails.Payload.Contact != null &&
                accountDetails.Payload.Contact.Length > 0)
            {
                _input.Show("Contact(s)", string.Join(", ", accountDetails.Payload.Contact));
            }
            else
            {
                _input.Show("Contact(s)", "(none)");
            }
            if (await _input.PromptYesNo("Modify contacts?", false))
            {
                try
                {
                    await _acmeClient.ChangeContacts();
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