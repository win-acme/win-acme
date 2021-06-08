using PKISharp.WACS.Configuration.Settings;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockSettingsService : ISettingsService
    {
        public Uri BaseUri => new("https://www.win-acme.com/");
        public UiSettings UI => new();
        public AcmeSettings Acme => new();
        public ProxySettings Proxy => new();
        public CacheSettings Cache => new();
        public ScheduledTaskSettings ScheduledTask => new();
        public NotificationSettings Notification => new();
        public SecuritySettings Security => new();
        public ClientSettings Client => new();
        public SourceSettings Source => new();
        public ValidationSettings Validation => new();
        public OrderSettings Order => new();
        public CsrSettings Csr => new();
        public StoreSettings Store => new();
        public InstallationSettings Installation => new();
        public ScriptSettings Script => new();
        public SecretsSettings Secrets => new();
    }
}
