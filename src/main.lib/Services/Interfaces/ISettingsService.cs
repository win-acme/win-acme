using System;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    {
        string ConfigPath { get; }
        string LogPath { get; }
        string CertificatePath { get; }
        string[] ClientNames { get; }
        int RenewalDays { get; }
        int HostsPerPage { get; }
        bool EncryptConfig { get; }
        TimeSpan ScheduledTaskStartBoundary { get; }
        TimeSpan ScheduledTaskRandomDelay { get; }
        TimeSpan ScheduledTaskExecutionTimeLimit { get; }
    }
}
