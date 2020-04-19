using System;
using static PKISharp.WACS.Services.SettingsService;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    { 
        string ExePath { get; }
        Uri BaseUri { get; }
        UiSettings UI { get; }
        AcmeSettings Acme { get; }
        ProxySettings Proxy { get; }
        CacheSettings Cache { get; } 
        ScheduledTaskSettings ScheduledTask { get; }
        NotificationSettings Notification { get; }
        SecuritySettings Security { get; }
        ScriptSettings Script { get; }
        ClientSettings Client { get; }
        TargetSettings Target { get; }
        ValidationSettings Validation { get; }
        OrderSettings Order { get; }
        CsrSettings Csr { get; }
        StoreSettings Store { get; }
        InstallationSettings Installation { get; }
    }
}
