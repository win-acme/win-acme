using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Configuration.Settings;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace PKISharp.WACS.Host.Services.Legacy
{
    public class LegacySettingsService : ISettingsService
    {
        private readonly ILogService _log;
        public ClientSettings Client { get; private set; } = new ClientSettings();
        public UiSettings UI { get; private set; } = new UiSettings();
        public AcmeSettings Acme { get; private set; } = new AcmeSettings();
        public ProxySettings Proxy { get; private set; } = new ProxySettings();
        public CacheSettings Cache { get; private set; } = new CacheSettings();
        public ScheduledTaskSettings ScheduledTask { get; private set; } = new ScheduledTaskSettings();
        public NotificationSettings Notification { get; private set; } = new NotificationSettings();
        public SecuritySettings Security { get; private set; } = new SecuritySettings();
        public ScriptSettings Script { get; private set; } = new ScriptSettings();
        public SourceSettings Source { get; private set; } = new SourceSettings();
        public ValidationSettings Validation { get; private set; } = new ValidationSettings();
        public OrderSettings Order { get; private set; } = new OrderSettings();
        public CsrSettings Csr { get; private set; } = new CsrSettings();
        public StoreSettings Store { get; private set; } = new StoreSettings();
        public InstallationSettings Installation { get; private set; } = new InstallationSettings();
        public SecretsSettings Secrets { get; private set; } = new SecretsSettings();
        public List<string> ClientNames { get; private set; }
        public Uri BaseUri { get; private set; } 

        public LegacySettingsService(ILogService log, MainArguments main, ISettingsService settings)
        {
            _log = log;
            UI = settings.UI;
            Acme = settings.Acme;
            ScheduledTask = settings.ScheduledTask;
            Notification = settings.Notification;
            Security = settings.Security;
            Script = settings.Script;
            // Copy so that the "ConfigurationPath" setting is not modified
            // from outside anymore
            Client = new ClientSettings()
            {
                ClientName = settings.Client.ClientName,
                ConfigurationPath = settings.Client.ConfigurationPath,
                LogPath = settings.Client.LogPath
            };
            Validation = settings.Validation;
            Store = settings.Store;

            ClientNames = new List<string>() { 
                settings.Client.ClientName,
                "win-acme", 
                "letsencrypt-win-simple"
            };

            // Read legacy configuration file
            var installDir = new FileInfo(VersionService.ExePath).DirectoryName;
            var legacyConfig = new FileInfo(Path.Combine(installDir!, "settings.config"));
            var userRoot = default(string);
            if (legacyConfig.Exists)
            {
                var configXml = new XmlDocument();
                configXml.LoadXml(File.ReadAllText(legacyConfig.FullName));

                // Get custom configuration path:
                var configPath = configXml.SelectSingleNode("//setting[@name='ConfigurationPath']/value")?.InnerText ?? "";
                if (!string.IsNullOrEmpty(configPath))
                {
                    userRoot = configPath;
                }

                // Get custom client name: 
                var customName = configXml.SelectSingleNode("//setting[@name='ClientName']/value")?.InnerText ?? "";
                if (!string.IsNullOrEmpty(customName))
                {
                    ClientNames.Insert(0, customName);
                }
            }
            BaseUri = new Uri(main.BaseUri);
            CreateConfigPath(main, userRoot);
        }

        private void CreateConfigPath(MainArguments options, string? userRoot)
        {
            var configRoot = "";
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
                        Client.ClientName = clientName;
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
                        foreach (var clientName in ClientNames.ToArray().Reverse())
                        {
                            Client.ClientName = clientName;
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

            Client.ConfigurationPath = Path.Combine(configRoot, CleanFileName(options.BaseUri));
            _log.Debug("Legacy config folder: {_configPath}", Client.ConfigurationPath);
        }

        public string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}