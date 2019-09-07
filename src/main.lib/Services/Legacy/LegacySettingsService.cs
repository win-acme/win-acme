using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Host.Services.Legacy
{
    public class LegacySettingsService : ISettingsService
    {
        private readonly List<string> _clientNames;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly MainArguments _arguments;
        public LegacySettingsService(ILogService log, MainArguments main, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
            _arguments = main;
            _clientNames = new List<string>() { "win-acme", "letsencrypt-win-simple" };
            var customName = Properties.Settings.Default.ClientName;
            if (!string.IsNullOrEmpty(customName))
            {
                _clientNames.Insert(0, customName);
            }
            CreateConfigPath(main);
        }

        public string ConfigPath { get; set; }
        public string CertificatePath { get; set; }
        public string[] ClientNames => _clientNames.ToArray();
        public int RenewalDays => _settings.RenewalDays;
        public int HostsPerPage => _settings.HostsPerPage;
        public TimeSpan ScheduledTaskStartBoundary => _settings.ScheduledTaskStartBoundary;
        public TimeSpan ScheduledTaskRandomDelay => _settings.ScheduledTaskRandomDelay;
        public TimeSpan ScheduledTaskExecutionTimeLimit => _settings.ScheduledTaskExecutionTimeLimit;
        public string LogPath => _settings.LogPath;
        public bool EncryptConfig => _settings.EncryptConfig;
        public string FileDateFormat => _settings.FileDateFormat;
        public string DefaultBaseUri => _settings.DefaultBaseUri;
        public string DefaultBaseUriTest => _settings.DefaultBaseUriTest;
        public string DefaultBaseUriImport => _settings.DefaultBaseUriImport;
        public int CertificateCacheDays => _settings.CertificateCacheDays;
        public bool DeleteStaleCacheFiles => _settings.DeleteStaleCacheFiles;
        public string Proxy => _settings.Proxy;
        public string ProxyUsername => _settings.ProxyUsername;
        public string ProxyPassword => _settings.ProxyPassword;
        public string SmtpServer => _settings.SmtpServer;
        public int SmtpPort => _settings.SmtpPort;
        public string SmtpUser => _settings.SmtpUser;
        public string SmtpPassword => _settings.SmtpPassword;
        public bool SmtpSecure => _settings.SmtpSecure;
        public string SmtpSenderName => _settings.SmtpSenderName;
        public string SmtpSenderAddress => _settings.SmtpSenderAddress;
        public string SmtpReceiverAddress => _settings.SmtpReceiverAddress;
        public bool EmailOnSuccess => _settings.EmailOnSuccess;
        public int RSAKeyBits => _settings.RSAKeyBits;
        public bool PrivateKeyExportable => _settings.PrivateKeyExportable;
        public bool CleanupFolders => _settings.CleanupFolders;
        public string DnsServer => _settings.DnsServer;
        public string DefaultCertificateStore => _settings.DefaultCertificateStore;
        public string DefaultCentralSslStore => _settings.DefaultCentralSslStore;
        public string DefaultCentralSslPfxPassword => _settings.DefaultCentralSslPfxPassword;
        public string DefaultPemFilesPath => _settings.DefaultPemFilesPath;
        public string ECCurve => _settings.ECCurve;

        private void CreateConfigPath(MainArguments options)
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

            ConfigPath = Path.Combine(configRoot, CleanFileName(options.BaseUri));
            _log.Debug("Legacy config folder: {_configPath}", ConfigPath);
        }

        public string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public string BaseUri => _settings.BaseUri;
    }
}