using Microsoft.Win32;
using System;
using LetsEncrypt.ACME.Simple.Services;
using System.IO;
using LetsEncrypt.ACME.Simple.Extensions;

namespace LetsEncrypt.ACME.Simple
{
    public class SettingsService
    {
        private string _configPath;
        private string _clientName;
        private ILogService _log;

        public SettingsService(string clientName, ILogService log, IOptionsService optionsService)
        {
            _log = log;
            _clientName = clientName;

            var settings = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings.config");
            var settingsTemplate = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings_default.config");
            if (!settings.Exists && settingsTemplate.Exists)
            {
                settingsTemplate.CopyTo(settings.FullName);
            }
            CreateConfigPath(optionsService.Options);
            _log.Verbose("Settings {@settings}", this);
        }

        public string ConfigPath
        {
            get { return _configPath; }
        }

        public string ClientName
        {
            get { return _clientName; }
        }

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
            // Path configured in settings always wins
            string configBasePath = Properties.Settings.Default.ConfigurationPath;

            if (string.IsNullOrWhiteSpace(configBasePath))
            {
                // The default folder location for compatibility with v1.9.4 and before is 
                // still the ApplicationData folder.
                configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // However, if that folder doesn't exist already (so we are either a new install
                // or a new user account), we choose the CommonApplicationData folder instead to
                // be more flexible in who runs the program (interactive or task scheduler).
                if (!Directory.Exists(Path.Combine(configBasePath, _clientName)))
                {
                    configBasePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                }
            }

            _configPath = Path.Combine(configBasePath, _clientName, options.BaseUri.CleanFileName());
            _log.Debug("Config folder: {_configPath}", _configPath);
            Directory.CreateDirectory(_configPath);
        }

    }
}