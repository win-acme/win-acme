using System;
using static PKISharp.WACS.Services.SettingsService;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    {        
        Uri BaseUri { get; }
        public UiSettings UI { get; }
        public AcmeSettings Acme { get; }
        public ScheduledTaskSettings ScheduledTask { get; }
        public NotificationSettings Notification { get; }
        public SecuritySettings Security { get; }
        public PathSettings Paths { get; }
        public ValidationSettings Validation { get; }
        public StoreSettings Store { get; }
    }
}
