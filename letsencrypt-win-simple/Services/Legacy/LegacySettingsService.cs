using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services.Legacy
{
    public class LegacySettingsService : ISettingsService
    {
        private List<string> _clientNames;
        private ILogService _log;

        public LegacySettingsService(ILogService log, IOptionsService optionsService)
        {
            _log = log;
            _clientNames = new List<string>() { "win-acme", "letsencrypt-win-simple" };
            var customName = Properties.Settings.Default.ClientName;
            if (!string.IsNullOrEmpty(customName))
            {
                _clientNames.Insert(0, customName);
            }
            CreateConfigPath(optionsService.Options);
        }

        public string ConfigPath { get; set; }

        public string[] ClientNames => _clientNames.ToArray();

        public int RenewalDays => 0;

        public int HostsPerPage => 0;

        public TimeSpan ScheduledTaskStartBoundary => new TimeSpan();

        public TimeSpan ScheduledTaskRandomDelay => new TimeSpan();

        public TimeSpan ScheduledTaskExecutionTimeLimit => new TimeSpan();

        private void CreateConfigPath(Options options)
        {
            var configRoot = "";
            var userRoot = Properties.Settings.Default.ConfigurationPath;
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

            ConfigPath = Path.Combine(configRoot, options.BaseUri.CleanFileName());
            _log.Debug("Legacy config folder: {_configPath}", ConfigPath);
        }

    }
}