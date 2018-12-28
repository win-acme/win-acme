using System;

namespace PKISharp.WACS.Services
{
    public interface ISettingsService
    {
        string ConfigPath { get; }
        string[] ClientNames { get; }
        int RenewalDays { get; }
        int HostsPerPage { get; }
        TimeSpan ScheduledTaskStartBoundary { get; }
        TimeSpan ScheduledTaskRandomDelay { get; }
        TimeSpan ScheduledTaskExecutionTimeLimit { get; }
    }
}
