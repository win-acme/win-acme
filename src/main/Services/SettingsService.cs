using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS
{
    public class SettingsService : ISettingsService
    {
        private List<string> _clientNames;
        private ILogService _log;

        public SettingsService(ILogService log, IOptionsService optionsService)
        { 
            _log = log;
            var settings = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings.config");
            var settingsTemplate = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings_default.config");
            if (!settings.Exists && settingsTemplate.Exists)
            {
                settingsTemplate.CopyTo(settings.FullName);
            }

            _clientNames = new List<string>() { "win-acme" };
            var customName = Properties.Settings.Default.ClientName;
            if (!string.IsNullOrEmpty(customName))
            {
                _clientNames.Insert(0, customName);
            }

            CreateConfigPath(optionsService.Options);
            _log.Verbose("Settings {@settings}", this);
        }

        public string ConfigPath { get; private set; }

        public string[] ClientNames => _clientNames.ToArray();

        public int RenewalDays
        {
            get
            {
                return ReadFromConfig(nameof(Properties.Settings.Default.RenewalDays),
                    55,
                    () => Properties.Settings.Default.RenewalDays);
            }
        }

        public int HostsPerPage
        {
            get
            {
                return ReadFromConfig(nameof(Properties.Settings.Default.HostsPerPage),
                    50,
                    () => Properties.Settings.Default.HostsPerPage);
            }
        }

        public TimeSpan ScheduledTaskRandomDelay
        {
            get
            {
                return ReadFromConfig(nameof(Properties.Settings.Default.ScheduledTaskRandomDelay),
                    new TimeSpan(0, 0, 0),
                    () => Properties.Settings.Default.ScheduledTaskRandomDelay);
            }
        }

        public TimeSpan ScheduledTaskStartBoundary 
        {
            get
            {
                return ReadFromConfig(nameof(Properties.Settings.Default.ScheduledTaskStartBoundary),
                    new TimeSpan(9, 0, 0),
                    () => Properties.Settings.Default.ScheduledTaskStartBoundary);
            }
        }

        public TimeSpan ScheduledTaskExecutionTimeLimit
        {
            get
            {
                return ReadFromConfig(nameof(Properties.Settings.Default.ScheduledTaskExecutionTimeLimit),
                    new TimeSpan(2, 0, 0),
                    () => Properties.Settings.Default.ScheduledTaskExecutionTimeLimit);
            }
        }

        private T ReadFromConfig<T>(string name, T defaultValue, Func<T> accessor)
        {
            try
            {
                return accessor();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting {name}, using default {default}", name, defaultValue);
            }
            return defaultValue;
        }

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

            ConfigPath = Path.Combine(configRoot, options.BaseUri.CleanBaseUri());
            _log.Debug("Config folder: {_configPath}", ConfigPath);
            Directory.CreateDirectory(ConfigPath);
        }

    }
}