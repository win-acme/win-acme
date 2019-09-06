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
        private readonly List<string> _clientNames;
        private readonly ILogService _log;

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
            CreateLogPath();
            CreateCertificatePath();
        }

        #region UI
        public int HostsPerPage
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.PageSize),
                    50,
                    () => Settings.Default.PageSize);
            }
        }
        public string FileDateFormat => Settings.Default.FileDateFormat;
        #endregion

        #region ACME
        public string DefaultBaseUri => Settings.Default.DefaultBaseUri;
        public string DefaultBaseUriTest => Settings.Default.DefaultBaseUriTest;
        public string DefaultBaseUriImport => Settings.Default.DefaultBaseUriImport;
        public int CertificateCacheDays => Settings.Default.CertificateCacheDays;
        public bool DeleteStaleCacheFiles => Settings.Default.DeleteStaleCacheFiles;
        public string Proxy => Settings.Default.Proxy;
        public string ProxyUsername => Settings.Default.ProxyUsername;
        public string ProxyPassword => Settings.Default.ProxyPassword;
        #endregion

        #region ScheduledTask
        public int RenewalDays
        {
            get
            {
                return ReadFromConfig(nameof(Settings.Default.RenewalDays),
                    55,
                    () => Settings.Default.RenewalDays);
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
        #endregion

        #region Notifications
        public string SmtpServer => Settings.Default.SmtpServer;
        public int SmtpPort => Settings.Default.SmtpPort;
        public string SmtpUser => Settings.Default.SmtpUser;
        public string SmtpPassword => Settings.Default.SmtpPassword;
        public bool SmtpSecure => Settings.Default.SmtpSecure;
        public string SmtpSenderName => Settings.Default.SmtpSenderName;
        public string SmtpSenderAddress => Settings.Default.SmtpSenderAddress;
        public string SmtpReceiverAddress => Settings.Default.SmtpReceiverAddress;
        public bool EmailOnSuccess => Settings.Default.EmailOnSuccess;
        #endregion

        #region Security
        public int RSAKeyBits
        {
            get
            {
                try
                {
                    if (Settings.Default.RSAKeyBits >= 2048)
                    {
                        _log.Debug("RSAKeyBits: {RSAKeyBits}", Settings.Default.RSAKeyBits);
                        return Settings.Default.RSAKeyBits;
                    }
                    else
                    {
                        _log.Warning("RSA key bits less than 2048 is not secure.");
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning("Unable to get RSA Key bits, error: {@ex}", ex);
                }
                return 2048;
            }
        }
        public string ECCurve => Settings.Default.ECCurve;
        public bool PrivateKeyExportable => Settings.Default.PrivateKeyExportable;
        public bool EncryptConfig => Settings.Default.EncryptConfig;
        #endregion

        #region Disk paths
        public string[] ClientNames => _clientNames.Distinct().ToArray();
        public string ConfigPath { get; private set; }
        public string CertificatePath { get; private set; }
        public string LogPath { get; private set; }
        #endregion

        #region Validation
        public bool CleanupFolders => Settings.Default.CleanupFolders;
        public string DnsServer => Settings.Default.DnsServer;
        #endregion

        #region Store
        public string DefaultCertificateStore => Settings.Default.DefaultCertificateStore;
        public string DefaultCentralSslStore => Settings.Default.DefaultCentralSslStore;
        public string DefaultCentralSslPfxPassword => Settings.Default.DefaultCentralSslPfxPassword;
        public string DefaultPemFilesPath => Settings.Default.DefaultPemFilesPath;
        #endregion

        /// <summary>
        /// Read value from configuration, might not be needed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <param name="accessor"></param>
        /// <returns></returns>
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
        private void CreateLogPath()
        {
            LogPath = Settings.Default.LogPath;
            if (string.IsNullOrWhiteSpace(LogPath))
            {
                LogPath = Path.Combine(ConfigPath, "Log");
            }
            if (!Directory.Exists(LogPath))
            {
                try
                {
                    Directory.CreateDirectory(LogPath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create log directory {_logPath}", LogPath);
                    throw;
                }
            }
            _log.Debug("Log path: {_logPath}", LogPath);
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