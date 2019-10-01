using Microsoft.Extensions.Configuration;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public UiSettings UI { get; private set; } = new UiSettings();
        public AcmeSettings Acme { get; private set; } = new AcmeSettings();
        public ScheduledTaskSettings ScheduledTask { get; private set; } = new ScheduledTaskSettings();
        public NotificationSettings Notification { get; private set; } = new NotificationSettings();
        public SecuritySettings Security { get; private set; } = new SecuritySettings();
        public PathSettings Paths { get; private set; } = new PathSettings();
        public ValidationSettings Validation { get; private set; } = new ValidationSettings();
        public StoreSettings Store { get; private set; } = new StoreSettings();

        public SettingsService(ILogService log, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;

            var installDir = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;
            _log.Verbose($"Looking for settings.json in {installDir}");
            var settings = new FileInfo(Path.Combine(installDir, "settings.json"));
            var settingsTemplate = new FileInfo(Path.Combine(installDir, "settings_default.json"));
            if (!settings.Exists && settingsTemplate.Exists)
            {
                _log.Verbose($"Copying settings_default.json to settings.json");
                settingsTemplate.CopyTo(settings.FullName);
            }

            new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(installDir, "settings.json"), true, true)
                .Build()
                .Bind(this);

            CreateConfigPath();
            CreateLogPath();
            CreateCertificatePath();
        }

        public Uri BaseUri
        {
            get
            {
                if (_arguments.MainArguments == null)
                {
                    return Acme.DefaultBaseUri;
                }
                return !string.IsNullOrEmpty(_arguments.MainArguments.BaseUri) ? 
                    new Uri(_arguments.MainArguments.BaseUri) :
                        _arguments.MainArguments.Test ? 
                            Acme.DefaultBaseUriTest : 
                            Acme.DefaultBaseUri;
            }
        }

        /// <summary>
        /// Find and/or create path of the configuration files
        /// </summary>
        /// <param name="arguments"></param>
        private void CreateConfigPath()
        {
            var configRoot = "";

            var userRoot = Paths.ConfigPath;
            if (!string.IsNullOrEmpty(userRoot))
            {
                configRoot = userRoot;

                // Path configured in settings always wins, but
                // check for possible sub directories with client name
                // to keep bug-compatible with older releases that
                // created a subfolder inside of the users chosen config path
                foreach (var clientName in Paths.ClientNames)
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
                    foreach (var clientName in Paths.ClientNames.AsEnumerable().Reverse())
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
            Paths.ConfigPath = Path.Combine(configRoot, BaseUri.ToString().CleanBaseUri());
            _log.Debug("Config folder: {_configPath}", Paths.ConfigPath);
            Directory.CreateDirectory(Paths.ConfigPath);
        }

        /// <summary>
        /// Find and/or created path of the certificate cache
        /// </summary>
        private void CreateLogPath()
        {
            if (string.IsNullOrWhiteSpace(Paths.LogPath))
            {
                Paths.LogPath = Path.Combine(Paths.ConfigPath, "Log");
            }
            if (!Directory.Exists(Paths.LogPath))
            {
                try
                {
                    Directory.CreateDirectory(Paths.LogPath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create log directory {_logPath}", Paths.LogPath);
                    throw;
                }
            }
            _log.Debug("Log path: {_logPath}", Paths.LogPath);
        }

        /// <summary>
        /// Find and/or created path of the certificate cache
        /// </summary>
        private void CreateCertificatePath()
        {
            Paths.CertificatePath = Paths.CertificatePath;
            if (string.IsNullOrWhiteSpace(Paths.CertificatePath))
            {
                Paths.CertificatePath = Path.Combine(Paths.ConfigPath, "Certificates");
            }
            if (!Directory.Exists(Paths.CertificatePath))
            {
                try
                {
                    Directory.CreateDirectory(Paths.CertificatePath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create certificate directory {_certificatePath}", Paths.CertificatePath);
                    throw;
                }
            }
            _log.Debug("Certificate cache: {_certificatePath}", Paths.CertificatePath);
        }

        public class UiSettings
        {
            /// <summary>
            /// The number of hosts to display per page.
            /// </summary>
            public int PageSize { get; set; }
            /// <summary>
            /// A string that is used to format the date of the 
            /// pfx file friendly name. Documentation for 
            /// possibilities is available from Microsoft.
            /// </summary>
            public string DateFormat { get; set; }
        }

        public class AcmeSettings
        {
            /// <summary>
            /// Default ACMEv2 endpoint to use when none 
            /// is specified with the command line.
            /// </summary>
            public Uri DefaultBaseUri { get; set; }
            /// <summary>
            /// Default ACMEv2 endpoint to use when none is specified 
            /// with the command line and the --test switch is
            /// activated.
            /// </summary>
            public Uri DefaultBaseUriTest { get; set; }
            /// <summary>
            /// Default ACMEv1 endpoint to import renewal settings from.
            /// </summary>
            public Uri DefaultBaseUriImport { get; set; }
            /// <summary>
            /// When renewing or re-creating a previously
            /// requested certificate that has the exact 
            /// same set of domain names, the program will 
            /// used a cached version for this many days,
            /// to prevent users from running into rate 
            /// limits while experimenting. Set this to 
            /// a high value if you regularly re-request 
            /// the same certificates, e.g. for a Continuous 
            /// Deployment scenario.
            /// </summary>
            public int CertificateCacheDays { get; set; }
            /// <summary>
            /// Automatically delete files older than 120 days 
            /// from the CertificatePath folder. Running with 
            /// default settings, these should only be long-
            /// expired certificates, generated for abandoned
            /// renewals. However we do advise caution.
            /// </summary>
            public bool DeleteStaleCacheFiles { get; set; }
            /// <summary>
            /// Configures a proxy server to use for 
            /// communication with the ACME server. The 
            /// default setting uses the system proxy.
            /// Passing an empty string will bypass the 
            /// system proxy.
            /// </summary>
            public string Proxy { get; set; }
            /// <summary>
            /// Username used to access the proxy server.
            /// </summary>
            public string ProxyUsername { get; set; }
            /// <summary>
            /// Password used to access the proxy server.
            /// </summary>
            public string ProxyPassword { get; set; }
        }

        public class ScheduledTaskSettings
        {
            /// <summary>
            /// The number of days to renew a certificate 
            /// after. Let’s Encrypt certificates are 
            /// currently for a max of 90 days so it is 
            /// advised to not increase the days much.
            /// If you increase the days, please note
            /// that you will have less time to fix any
            /// issues if the certificate doesn’t renew 
            /// correctly.
            /// </summary>
            public int RenewalDays { get; set; }
            /// <summary>
            /// Configures random time to wait for starting 
            /// the scheduled task.
            /// </summary>
            public TimeSpan RandomDelay { get; set; }
            /// <summary>
            /// Configures start time for the scheduled task.
            /// </summary>
            public TimeSpan StartBoundary { get; set; }
            /// <summary>
            /// Configures time after which the scheduled 
            /// task will be terminated if it hangs for
            /// whatever reason.
            /// </summary>
            public TimeSpan ExecutionTimeLimit { get; set; }
        }

        public class NotificationSettings
        {
            /// <summary>
            /// SMTP server to use for sending email notifications. 
            /// Required to receive renewal failure notifications.
            /// </summary>
            public string SmtpServer { get; set; }
            /// <summary>
            /// SMTP server port number.
            /// </summary>
            public int SmtpPort { get; set; }
            /// <summary>
            /// User name for the SMTP server, in case 
            /// of authenticated SMTP.
            /// </summary>
            public string SmtpUser { get; set; }
            /// <summary>
            /// Password for the SMTP server, in case 
            /// of authenticated SMTP.
            /// </summary>
            public string SmtpPassword { get; set; }
            /// <summary>
            /// Change to True to enable SMTPS.
            /// </summary>
            public bool SmtpSecure { get; set; }
            /// <summary>
            /// Display name to use as the sender of 
            /// notification emails. Defaults to the 
            /// ClientName setting when empty.
            /// </summary>
            public string SenderName { get; set; }
            /// <summary>
            /// Email address to use as the sender 
            /// of notification emails. Required to 
            /// receive renewal failure notifications.
            /// </summary>
            public string SenderAddress { get; set; }
            /// <summary>
            /// Email addresses to receive notification emails. 
            /// Required to receive renewal failure 
            /// notifications.
            /// </summary>
            public List<string> ReceiverAddresses { get; set; }
            /// <summary>
            /// Send an email notification when a certificate 
            /// has been successfully renewed, as opposed to 
            /// the default behavior that only send failure
            /// notifications. Only works if at least 
            /// SmtpServer, SmtpSenderAddressand 
            /// SmtpReceiverAddress have been configured.
            /// </summary>
            public bool EmailOnSuccess { get; set; }
        }

        public class SecuritySettings
        {
            /// <summary>
            /// The key size to sign the certificate with. 
            /// Minimum is 2048.
            /// </summary>
            public int RSAKeyBits { get; set; }
            /// <summary>
            /// The curve to use for EC certificates.
            /// </summary>
            public string ECCurve { get; set; }
            /// <summary>
            /// If set to True, it will be possible to export 
            /// the generated certificates from the certificate 
            /// store, for example to move them to another 
            /// server.
            /// </summary>
            public bool PrivateKeyExportable { get; set; }
            /// <summary>
            /// Uses Microsoft Data Protection API to encrypt 
            /// sensitive parts of the configuration, e.g. 
            /// passwords. This may be disabled to share 
            /// the configuration across a cluster of machines.
            /// </summary>
            public bool EncryptConfig { get; set; }
        }

        public class PathSettings
        {
            public List<string> ClientNames { get; set; } = new List<string> { "win-acme" };
            public string ConfigPath { get; set; }
            public string CertificatePath { get; set; }
            public string LogPath { get; set; }
        }

        public class ValidationSettings
        {
            public bool CleanupFolders { get; set; }
            public List<string> DnsServers { get; set; }
        }

        public class StoreSettings
        {
            public string DefaultCertificateStore { get; set; }
            public string DefaultCentralSslStore { get; set; }
            public string DefaultCentralSslPfxPassword { get; set; }
            public string DefaultPemFilesPath { get; set; }
        }
    }
}