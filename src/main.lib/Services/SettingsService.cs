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

        public SettingsService(ILogService log, MainArguments arguments)
        {
            _log = log;
            _arguments = arguments;
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

            var configRoot = ChooseConfigPath();
            Client.ConfigurationPath = Path.Combine(configRoot, BaseUri.CleanUri()!);
            Client.LogPath = ChooseLogPath();
            Cache.Path = ChooseCachePath();

            EnsureFolderExists(configRoot, "configuration", true);
            EnsureFolderExists(Client.ConfigurationPath, "configuration", false);
            EnsureFolderExists(Client.LogPath, "log", !Client.LogPath.StartsWith(Client.ConfigurationPath));
            EnsureFolderExists(Cache.Path, "cache", !Client.LogPath.StartsWith(Client.ConfigurationPath));

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
        private string ChooseConfigPath()
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
            return configRoot;
        }

        /// <summary>
        /// Determine which folder to use for logging
        /// </summary>
        private string ChooseLogPath()
        {
            if (string.IsNullOrWhiteSpace(Client.LogPath))
            {
                return Path.Combine(Client.ConfigurationPath, "Log");
            }
            else
            {
                // Create seperate logs for each endpoint
                return Path.Combine(Client.LogPath, BaseUri.CleanUri()!);
            }
        }

        /// <summary>
        /// Determine which folder to use for cache certificates
        /// </summary>
        private string ChooseCachePath()
        {
            if (string.IsNullOrWhiteSpace(Cache.Path))
            {
                return Path.Combine(Client.ConfigurationPath, "Certificates");
            }
            return Cache.Path;
        }

        /// <summary>
        /// Create folder if needed
        /// </summary>
        /// <param name="path"></param>
        /// <param name="label"></param>
        /// <exception cref="Exception"></exception>
        private void EnsureFolderExists(string path, string label, bool checkAcl)
        {
            var created = false;
            var di = new DirectoryInfo(path);
            if (!di.Exists)
            {
                try
                {
                    di = Directory.CreateDirectory(path);
                    _log.Debug($"Created {label} folder {{path}}", path);
                    created = true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to create {label} {path}", ex);
                }
            }
            else
            {
                _log.Debug($"Use existing {label} folder {{path}}", path);
            }
            if (checkAcl)
            {
                EnsureFolderAcl(di, label, created);
            }
        }

        /// <summary>
        /// Ensure proper access rights to a folder
        /// </summary>
        private void EnsureFolderAcl(DirectoryInfo di, string label, bool created)
        {
            // Test access control rules
            var (access, inherited) = UsersHaveAccess(di);
            if (!access)
            {
                return;
            }

            if (!created)
            {
                _log.Warning("All users currently have access to {path}.", di.FullName);
                _log.Warning("That access will now be limited to improve security.", label, di.FullName);
                _log.Warning("You may manually add specific trusted accounts to the ACL.", label, di.FullName);
            }

            var acl = di.GetAccessControl();
            if (inherited)
            {
                // Disable access rule inheritance
                acl.SetAccessRuleProtection(true, true);
                di.SetAccessControl(acl);
                acl = di.GetAccessControl();
            }

            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference == sid && 
                    rule.AccessControlType == AccessControlType.Allow)
                {
                    acl.RemoveAccessRule(rule);
                }
            }
            di.SetAccessControl(acl);
        }

        /// <summary>
        /// Test if users have access through inherited or direct rules
        /// </summary>
        /// <param name="di"></param>
        /// <returns></returns>
        private static (bool, bool) UsersHaveAccess(DirectoryInfo di)
        {
            var acl = di.GetAccessControl();
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var hit = false;
            var inherited = false;
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference == sid &&
                    rule.AccessControlType == AccessControlType.Allow)
                {
                    hit = true;
                    inherited = inherited || rule.IsInherited;
                }
            }
            return (hit, inherited);
        }
    }
}