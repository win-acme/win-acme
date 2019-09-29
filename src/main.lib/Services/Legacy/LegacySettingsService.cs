using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PKISharp.WACS.Services.SettingsService;

namespace PKISharp.WACS.Host.Services.Legacy
{
    public class LegacySettingsService : ISettingsService
    {
        private readonly List<string> _clientNames;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public UiSettings UI { get; private set; }
        public AcmeSettings Acme { get; private set; }
        public ScheduledTaskSettings ScheduledTask { get; private set; }
        public NotificationSettings Notification { get; private set; }
        public SecuritySettings Security { get; private set; }
        public DiskPathSettings Paths { get; private set; }
        public ValidationSettings Validation { get; private set; }
        public StoreSettings Store { get; private set; }

        public LegacySettingsService(ILogService log, MainArguments main, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
            UI = settings.UI;
            Acme = settings.Acme;
            ScheduledTask = settings.ScheduledTask;
            Notification = settings.Notification;
            Security = settings.Security;
            Paths = settings.Paths;
            Validation = settings.Validation;
            Store = settings.Store;
            _clientNames = new List<string>() { "win-acme", "letsencrypt-win-simple" };
            CreateConfigPath(main);
        }

        public string ConfigPath { get; set; }
        public string CertificatePath { get; set; }
        public string[] ClientNames => _clientNames.ToArray();
      
        private void CreateConfigPath(MainArguments options)
        {
            var configRoot = "";
            var userRoot = default(string);
            if (!string.IsNullOrEmpty(userRoot))
            {
                configRoot = userRoot;

                // Path configured in settings always wins, but
                // check for possible sub directories with client name
                // to keep bug-compatible with older releases that
                // created a subfolder inside of the users chosen config path
                foreach (var clientName in ClientNames)
                {
                    var configRootWithClient = Path.Combine(userRoot, clientName);
                    if (Directory.Exists(configRootWithClient))
                    {
                        configRoot = configRootWithClient;
                        break;
                    }
                }
            }
            else
            {
                // When using a system folder, we have to create a sub folder
                // with the most preferred client name, but we should check first
                // if there is an older folder with an less preferred (older)
                // client name.
                var roots = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                };
                foreach (var root in roots)
                {
                    // Stop looking if the directory has been found
                    if (!Directory.Exists(configRoot))
                    {
                        foreach (var clientName in ClientNames.Reverse())
                        {
                            configRoot = Path.Combine(root, clientName);
                            if (Directory.Exists(configRoot))
                            {
                                // Stop looking if the directory has been found
                                break;
                            }
                        }
                    }
                }
            }

            ConfigPath = Path.Combine(configRoot, CleanFileName(options.BaseUri));
            _log.Debug("Legacy config folder: {_configPath}", ConfigPath);
        }

        public string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

        public Uri BaseUri => _settings.BaseUri;
    }
}