using Microsoft.Extensions.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Configuration.Settings;
using PKISharp.WACS.Extensions;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PKISharp.WACS.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly AdminService _adminService;
        private readonly ILogService _log;
        private readonly MainArguments _arguments;

        public bool Valid { get; private set; } = false;
        public ClientSettings Client { get; private set; } = new ClientSettings();
        public UiSettings UI { get; private set; } = new UiSettings();
        public AcmeSettings Acme { get; private set; } = new AcmeSettings();
        public ExecutionSettings Execution { get; private set; } = new ExecutionSettings();
        public ProxySettings Proxy { get; private set; } = new ProxySettings();
        public CacheSettings Cache { get; private set; } = new CacheSettings();
        public ScheduledTaskSettings ScheduledTask { get; private set; } = new ScheduledTaskSettings();
        public NotificationSettings Notification { get; private set; } = new NotificationSettings();
        public SecuritySettings Security { get; private set; } = new SecuritySettings();
        public ScriptSettings Script { get; private set; } = new ScriptSettings();
        [Obsolete]
        public SourceSettings Target { get; private set; } = new SourceSettings();
        public SourceSettings Source { get; private set; } = new SourceSettings();
        public ValidationSettings Validation { get; private set; } = new ValidationSettings();
        public OrderSettings Order { get; private set; } = new OrderSettings();
        public CsrSettings Csr { get; private set; } = new CsrSettings();
        public StoreSettings Store { get; private set; } = new StoreSettings();
        public InstallationSettings Installation { get; private set; } = new InstallationSettings();
        public SecretsSettings Secrets { get; private set; } = new SecretsSettings();

        public SettingsService(ILogService log, AdminService adminService, MainArguments arguments)
        {
            _log = log;
            _arguments = arguments;
            _adminService = adminService;
            var settingsFileName = "settings.json";
            var settingsFileTemplateName = "settings_default.json";
            _log.Verbose("Looking for {settingsFileName} in {path}", settingsFileName, VersionService.SettingsPath);
            var settings = new FileInfo(Path.Combine(VersionService.SettingsPath, settingsFileName));
            var settingsTemplate = new FileInfo(Path.Combine(VersionService.ResourcePath, settingsFileTemplateName));
            var useFile = settings;
            if (!settings.Exists)
            {
                if (!settingsTemplate.Exists)
                {
                    // For .NET tool case
                    settingsTemplate = new FileInfo(Path.Combine(VersionService.ResourcePath, settingsFileName));
                }
                if (!settingsTemplate.Exists)
                {
                    _log.Warning("Unable to locate {settings}", settingsFileName);
                } 
                else
                {
                    _log.Verbose("Copying {settingsFileTemplateName} to {settingsFileName}", settingsFileTemplateName, settingsFileName);
                    try
                    {
                        if (!settings.Directory!.Exists)
                        {
                            settings.Directory.Create();
                        }
                        settingsTemplate.CopyTo(settings.FullName);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to create {settingsFileName}, falling back to defaults", settingsFileName);
                        useFile = settingsTemplate;
                    }
                }
            }

            try
            {
                new ConfigurationBuilder()
                    .AddJsonFile(useFile.FullName, true, true)
                    .Build()
                    .Bind(this);

                // This code specifically deals with backwards compatibility 
                // so it is allowed to use obsolete properties
#pragma warning disable CS0612
                static string? Fallback(string? x, string? y) => string.IsNullOrWhiteSpace(x) ? y : x;
                Source.DefaultSource = Fallback(Source.DefaultSource, Target.DefaultTarget);
                Store.PemFiles.DefaultPath = Fallback(Store.PemFiles.DefaultPath, Store.DefaultPemFilesPath);
                Store.CentralSsl.DefaultPath = Fallback(Store.CentralSsl.DefaultPath, Store.DefaultCentralSslStore);
                Store.CentralSsl.DefaultPassword = Fallback(Store.CentralSsl.DefaultPassword, Store.DefaultCentralSslPfxPassword);
                Store.CertificateStore.DefaultStore = Fallback(Store.CertificateStore.DefaultStore, Store.DefaultCertificateStore);
#pragma warning restore CS0612 
            }
            catch (Exception ex)
            {
                _log.Error($"Unable to start program using {useFile.Name}");
                while (ex.InnerException != null)
                {
                    _log.Error(ex.InnerException.Message);
                    ex = ex.InnerException;
                }
                return;
            }

            ChooseConfigPath();
            CreateConfigPath();
            CreateLogPath();
            CreateCachePath();
            Valid = true;
        }

        public Uri BaseUri
        {
            get
            {
                Uri? ret;
                if (!string.IsNullOrEmpty(_arguments.BaseUri))
                {
                    ret = new Uri(_arguments.BaseUri);
                }
                else if (_arguments.Test)
                {
                    ret = Acme.DefaultBaseUriTest;
                }
                else
                {
                    ret = Acme.DefaultBaseUri;
                }
                if (ret == null)
                {
                    throw new Exception("Unable to determine BaseUri");
                }
                return ret;
            }
        }

        /// <summary>
        /// Determine which folder to use for configuration data
        /// </summary>
        private void ChooseConfigPath()
        {
            var userRoot = Client.ConfigurationPath;
            string? configRoot;
            if (!string.IsNullOrEmpty(userRoot))
            {
                configRoot = userRoot;

                // Path configured in settings always wins, but
                // check for possible sub directories with client name
                // to keep bug-compatible with older releases that
                // created a subfolder inside of the users chosen config path
                var configRootWithClient = Path.Combine(userRoot, Client.ClientName);
                if (Directory.Exists(configRootWithClient))
                {
                    configRoot = configRootWithClient;
                }
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                configRoot = Path.Combine(appData, Client.ClientName);
            }

            // This only happens when invalid options are provided 
            Client.ConfigurationPath = Path.Combine(configRoot, BaseUri.CleanUri()!);
        }

        /// <summary>
        /// Create and/or configure the configuration folder
        /// </summary>
        /// <param name="arguments"></param>
        private void CreateConfigPath()
        {
            var created = false;
            var di = new DirectoryInfo(Client.ConfigurationPath);
            if (!di.Exists)
            {
                try
                {
                    di = Directory.CreateDirectory(Client.ConfigurationPath);
                    _log.Debug("Created configuration folder {folder}", Client.ConfigurationPath);
                    created = true;
                } 
                catch (Exception ex)
                {
                    throw new Exception($"Unable to create configuration folder {Client.ConfigurationPath}", ex);
                }
            } 
            else
            {
                _log.Debug("Use existing configuration folder {folder}", Client.ConfigurationPath);
            }

            // Test access control rules
            var acl = di.GetAccessControl();
            if (created)
            {
                // Disable access rule inheritance
                acl.SetAccessRuleProtection(true, true);
                di.SetAccessControl(acl);
                acl = di.GetAccessControl();
            }

            // Remove or warn about access by the "Users" group
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference == sid)
                {
                    if (created)
                    {
                        acl.RemoveAccessRule(rule);
                    } 
                    else
                    {
                        _log.Warning("All users have {FileSystemRights} access to the configuration folder", rule.FileSystemRights);
                    }
                }
            }
            if (created)
            {
                di.SetAccessControl(acl);
            }
        }

        /// <summary>
        /// Find and/or created path for logging
        /// </summary>
        private void CreateLogPath()
        {
            if (string.IsNullOrWhiteSpace(Client.LogPath))
            {
                Client.LogPath = Path.Combine(Client.ConfigurationPath, "Log");
            }
            else
            {
                // Create seperate logs for each endpoint
                Client.LogPath = Path.Combine(Client.LogPath, BaseUri.CleanUri()!);
            }
            if (!Directory.Exists(Client.LogPath))
            {
                try
                {
                    Directory.CreateDirectory(Client.LogPath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create log directory {_logPath}", Client.LogPath);
                    throw;
                }
            }
            _log.Debug("Log path: {_logPath}", Client.LogPath);
        }

        /// <summary>
        /// Find and/or created path of the certificate cache
        /// </summary>
        private void CreateCachePath()
        {
            if (string.IsNullOrWhiteSpace(Cache.Path))
            {
                Cache.Path = Path.Combine(Client.ConfigurationPath, "Certificates");
            }
            if (!Directory.Exists(Cache.Path))
            {
                try
                {
                    Directory.CreateDirectory(Cache.Path);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create cache path {_certificatePath}", Cache.Path);
                    throw;
                }
            }
            _log.Debug("Cache path: {_certificatePath}", Cache.Path);
        }

    }
}