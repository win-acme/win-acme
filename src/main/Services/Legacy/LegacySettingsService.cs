using PKISharp.WACS.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services.Legacy
{
    public class LegacySettingsService : ISettingsService
    {
        private readonly List<string> _clientNames;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public LegacySettingsService(ILogService log, MainArguments main, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
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

        public string LogPath => throw new NotImplementedException();

        public bool EncryptConfig => throw new NotImplementedException();

        public string FileDateFormat => throw new NotImplementedException();

        public string DefaultBaseUri => throw new NotImplementedException();

        public string DefaultBaseUriTest => throw new NotImplementedException();

        public string DefaultBaseUriImport => throw new NotImplementedException();

        public int CertificateCacheDays => throw new NotImplementedException();

        public bool DeleteStaleCacheFiles => throw new NotImplementedException();

        public string Proxy => throw new NotImplementedException();

        public string ProxyUsername => throw new NotImplementedException();

        public string ProxyPassword => throw new NotImplementedException();

        public string SmtpServer => throw new NotImplementedException();

        public int SmtpPort => throw new NotImplementedException();

        public string SmtpUser => throw new NotImplementedException();

        public string SmtpPassword => throw new NotImplementedException();

        public bool SmtpSecure => throw new NotImplementedException();

        public string SmtpSenderName => throw new NotImplementedException();

        public string SmtpSenderAddress => throw new NotImplementedException();

        public string SmtpReceiverAddress => throw new NotImplementedException();

        public bool EmailOnSuccess => throw new NotImplementedException();

        public int RSAKeyBits => throw new NotImplementedException();

        public bool PrivateKeyExportable => throw new NotImplementedException();

        public bool CleanupFolders => throw new NotImplementedException();

        public string DnsServer => throw new NotImplementedException();

        public string DefaultCertificateStore => throw new NotImplementedException();

        public string DefaultCentralSslStore => throw new NotImplementedException();

        public string DefaultCentralSslPfxPassword => throw new NotImplementedException();

        public string DefaultPemFilesPath => throw new NotImplementedException();

        public string ECCurve => throw new NotImplementedException();

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

    }
}