using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Host
{
    internal class Wacs
    {
        private readonly IInputService _input;
        private readonly IIISClient _iis;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly AdminService _adminService;
        private readonly AcmeClient _acmeClient;
        private readonly UpdateClient _updateClient;
        private readonly ArgumentsParser _arguments;
        private readonly ExceptionHandler _exceptionHandler;
        private readonly MainArguments _args;
        private readonly RenewalManager _renewalManager;
        private readonly RenewalCreator _renewalCreator;
        private readonly TaskSchedulerService _taskScheduler;
        private readonly VersionService _versionService;
        private readonly MainMenu _mainMenu;

        public Wacs(
            ExceptionHandler exceptionHandler,
            IIISClient iis,
            UpdateClient updateClient,
            AcmeClient acmeClient,
            ILogService logService,
            IInputService inputService,
            ISettingsService settingsService,
            VersionService versionService,
            ArgumentsParser argumentsParser,
            AdminService adminService,
            RenewalCreator renewalCreator,
            RenewalManager renewalManager,
            TaskSchedulerService taskSchedulerService,
            MainMenu mainMenu)
        {
            // Basic services
            _exceptionHandler = exceptionHandler;
            _log = logService;
            _settings = settingsService;
            _acmeClient = acmeClient;
            _updateClient = updateClient;
            _adminService = adminService;
            _taskScheduler = taskSchedulerService;
            _renewalCreator = renewalCreator; 
            _renewalManager = renewalManager;
            _arguments = argumentsParser;
            _input = inputService;
            _versionService = versionService;
            _mainMenu = mainMenu;
            _iis = iis;

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

            _arguments.ShowCommandLine();
            _args = _arguments.GetArguments<MainArguments>() ?? new MainArguments();
        }

        /// <summary>
        /// Main program
        /// </summary>
        public async Task<int> Start()
        {
            // Exit when settings are not valid. The settings service
            // also checks the command line arguments
            if (!_settings.Valid)
            {
                return -1;
            }
            if (!_versionService.Init())
            {
                return -1;
            }

            // Show informational message and start-up diagnostics
            await ShowBanner();

            // Version display
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
                        await _mainMenu.Import(RunLevel.Unattended);
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
                        await _mainMenu.Encrypt(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else if (_args.SetupTaskScheduler)
                    {
                        await _taskScheduler.CreateTaskScheduler(RunLevel.Unattended);
                        await CloseDefault();
                    }
                    else
                    {
                        await _mainMenu.MainMenuEntry(_args.Test ? RunLevel.Test : RunLevel.None);
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
                    _iis.Refresh();
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
                await _updateClient.CheckNewVersion();
            }

            _log.Information("Connecting to {ACME}...", _settings.BaseUri);
            await _acmeClient.CheckNetwork().ConfigureAwait(false);

            if (_adminService.IsAdmin)
            {
                _log.Debug("Running with administrator credentials");
                var iis = _iis.Version;
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
    }
}