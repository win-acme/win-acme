using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Properties;
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

        public SettingsService(ILogService log, IArgumentsService arguments)
        { 
            _log = log;
            var settings = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings.config");
            var settingsTemplate = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "settings_default.config");
            if (!settings.Exists && settingsTemplate.Exists)
            {
                settingsTemplate.CopyTo(settings.FullName);
            }

            _clientNames = new List<string>() { "win-acme" };
            var customName = Settings.Default.ClientName;
            if (!string.IsNullOrEmpty(customName))
            {
                _clientNames.Insert(0, customName);
            }

            CreateConfigPath(arguments.MainArguments);
            CreateCertificatePath();
            _log.Verbose("Settings {@settings}", this);
        }

        /// <summary>
        /// Path of main configuration data (ACME registration and renewals)
        /// </summary>
        public string ConfigPath { get; private set; }

        /// <summary>
        /// Path to the certificate cache
        /// </summary>
        public string CertificatePath { get; private set; }

        /// <summary>
        /// Names of the client
        /// </summary>
        public string[] ClientNames => _clientNames.ToArray();

        public int RenewalDays
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.RenewalDays), 
                    55, 
                    () => Settings.Default.RenewalDays);
            }
        }

        public int HostsPerPage
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.PageSize),
                    50,
                    () => Settings.Default.PageSize);
            }
        }

        public TimeSpan ScheduledTaskRandomDelay
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.ScheduledTaskRandomDelay),
                    new TimeSpan(0, 0, 0),
                    () => Settings.Default.ScheduledTaskRandomDelay);
            }
        }

        public TimeSpan ScheduledTaskStartBoundary 
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.ScheduledTaskStartBoundary),
                    new TimeSpan(9, 0, 0),
                    () => Settings.Default.ScheduledTaskStartBoundary);
            }
        }

        public TimeSpan ScheduledTaskExecutionTimeLimit
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.ScheduledTaskExecutionTimeLimit),
                    new TimeSpan(2, 0, 0),
                    () => Settings.Default.ScheduledTaskExecutionTimeLimit);
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

        /// <summary>
        /// Find and/or create path of the configuration files
        /// </summary>
        /// <param name="options"></param>
        private void CreateConfigPath(MainArguments options)
        {
            var configRoot = "";

            var userRoot = Settings.Default.ConfigurationPath;
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

                // Stop looking if the directory has been found
                if (!Directory.Exists(configRoot))
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    foreach (var clientName in ClientNames.Reverse())
                    {
                        configRoot = Path.Combine(appData, clientName);
                        if (Directory.Exists(configRoot))
                        {
                            // Stop looking if the directory has been found
                            break;
                        }
                    }
                }
            }

            // This only happens when invalid options are provided 
            if (options != null)
            {
                ConfigPath = Path.Combine(configRoot, options.GetBaseUri().CleanBaseUri());
                _log.Debug("Config folder: {_configPath}", ConfigPath);
                Directory.CreateDirectory(ConfigPath);
            }
        }

        /// <summary>
        /// Find and/or created path of the certificate cache
        /// </summary>
        private void CreateCertificatePath()
        {
            CertificatePath = Settings.Default.CertificatePath;
            if (string.IsNullOrWhiteSpace(CertificatePath))
            {
                CertificatePath = Path.Combine(ConfigPath, "Certificates");
            }
            if (!Directory.Exists(CertificatePath))
            {
                try
                {
                    Directory.CreateDirectory(CertificatePath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create certificate directory {_certificatePath}", CertificatePath);
                    throw;
                }
            }
            _log.Debug("Certificate cache: {_certificatePath}", CertificatePath);
        }
    }
}