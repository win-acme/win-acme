using System;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockSettingsService : ISettingsService
    {
        public Uri BaseUri => new Uri("https://pkisharp.github.io/win-acme/");
        public SettingsService.UiSettings UI => new SettingsService.UiSettings();
        public SettingsService.AcmeSettings Acme => new SettingsService.AcmeSettings();
        public SettingsService.ScheduledTaskSettings ScheduledTask => new SettingsService.ScheduledTaskSettings();
        public SettingsService.NotificationSettings Notification => new SettingsService.NotificationSettings();
        public SettingsService.SecuritySettings Security => new SettingsService.SecuritySettings();
        public SettingsService.PathSettings Paths => new SettingsService.PathSettings();
        public SettingsService.ValidationSettings Validation => new SettingsService.ValidationSettings();
        public SettingsService.StoreSettings Store => new SettingsService.StoreSettings();
    }
}
