namespace LetsEncrypt.ACME.Simple
{
    public interface ISettingsService
    {
        string ConfigPath { get;  }
        string[] RenewalStore { get; set; }
        int HostsPerPage { get; }
        int ScheduledTaskHour { get; }
    }
}