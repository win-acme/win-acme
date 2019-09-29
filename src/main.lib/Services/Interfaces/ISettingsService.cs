using System;
using static PKISharp.WACS.Services.SettingsService;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// Get BaseUri to use  
        /// </summary>
        /// <param name="options"></param>
        Uri BaseUri { get; }
        string ConfigPath { get; }
        string CertificatePath { get; }
        string[] ClientNames { get; }
        public UiSettings UI { get; }
        public AcmeSettings Acme { get; }
        public ScheduledTaskSettings ScheduledTask { get; }
        public NotificationSettings Notification { get; }
        public SecuritySettings Security { get; }
        public DiskPathSettings Paths { get; }
        public ValidationSettings Validation { get; }
        public StoreSettings Store { get; }
    }
}
