using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
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
        public ProxySettings Proxy { get; private set; }
        public CacheSettings Cache { get; private set; }
        public ScheduledTaskSettings ScheduledTask { get; private set; }
        public NotificationSettings Notification { get; private set; }
        public SecuritySettings Security { get; private set; }
        public ScriptSettings Script { get; private set; }
        public ClientSettings Client { get; private set; }
        public ValidationSettings Validation { get; private set; }
        public StoreSettings Store { get; private set; }
        public string ExePath { get; private set; }

        public Uri BaseUri { get; private set; } 

        public LegacySettingsService(ILogService log, MainArguments main, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
            UI = settings.UI;
            Acme = settings.Acme;
            ScheduledTask = settings.ScheduledTask;
            Notification = settings.Notification;
            Security = settings.Security;
            Script = settings.Script;
            Client = settings.Client;
            Validation = settings.Validation;
            Store = settings.Store;
            ExePath = settings.ExePath;

            _clientNames = new List<string>() { 
                settings.Client.ClientName,
                "win-acme", 
                "letsencrypt-win-simple"
            };

            // Read legacy configuration file
            var installDir = new FileInfo(ExePath).DirectoryName;
            var legacyConfig = new FileInfo(Path.Combine(installDir, "settings.config"));
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
                    _clientNames.Insert(0, customName);
                }
            }
            BaseUri = new Uri(main.ImportBaseUri);
            CreateConfigPath(main, userRoot);
        }

        private void CreateConfigPath(MainArguments options, string userRoot)
        {
            var configRoot = "";
            if (!string.IsNullOrEmpty(userRoot))
            {
                configRoot = userRoot;

                // Path configured in settings always wins, but
                // check for possible sub directories with client name
                // to keep bug-compatible with older releases that
                // created a subfolder inside of the users chosen config path
                foreach (var clientName in _clientNames)
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
                        foreach (var clientName in _clientNames.ToArray().Reverse())
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

            Client.ConfigurationPath = Path.Combine(configRoot, CleanFileName(options.ImportBaseUri));
            _log.Debug("Legacy config folder: {_configPath}", Client.ConfigurationPath);
        }

        public string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}