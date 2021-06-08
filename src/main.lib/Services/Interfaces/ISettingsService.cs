using PKISharp.WACS.Configuration.Settings;
using System;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    { 
        Uri BaseUri { get; }
        UiSettings UI { get; }
        AcmeSettings Acme { get; }
        ProxySettings Proxy { get; }
        CacheSettings Cache { get; }
        SecretsSettings Secrets { get; }
        ScheduledTaskSettings ScheduledTask { get; }
        NotificationSettings Notification { get; }
        SecuritySettings Security { get; }
        ScriptSettings Script { get; }
        ClientSettings Client { get; }
        SourceSettings Source { get; }
        ValidationSettings Validation { get; }
        OrderSettings Order { get; }
        CsrSettings Csr { get; }
        StoreSettings Store { get; }
        InstallationSettings Installation { get; }
    }
}
