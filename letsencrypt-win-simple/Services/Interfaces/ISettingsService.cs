using System;

namespace LetsEncrypt.ACME.Simple
{
    public interface ISettingsService
    {
        string ConfigPath { get;  }
        string[] RenewalStore { get; set; }
        int HostsPerPage { get; }

        TimeSpan ScheduledTaskStartBoundary { get; }
        TimeSpan ScheduledTaskExecutionTimeLimit { get; }
        TimeSpan ScheduledTaskRandomDelay { get; }
    }
}