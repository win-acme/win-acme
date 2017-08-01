using System.Collections.Generic;
using Microsoft.Win32;

namespace LetsEncrypt.ACME.Simple
{
    public class Settings
    {

        public const int maxNames = 100;

        //public bool AgreedToTOS { get; set; }
        //public string ContactEmail { get; set; }

        //public string ContactEmail { get; set; }
        //public string EmailServer { get; set; }
        //public string EmailUser { get; set; }
        //public string EmailPassword { get; set; }

        public string ScheduledTaskName
        {
            get { return Registry.GetValue(registryKey, "ScheduledTaskName", null) as string; }
            set { Registry.SetValue(registryKey, "ScheduledTaskName", value); }
        }

        string registryKey;

        public Settings(string clientName, string cleanBaseUri)
        {
            registryKey = $"HKEY_CURRENT_USER\\Software\\{clientName}\\{cleanBaseUri}";
        }

        const string renewalsValueName = "Renewals";

        public List<ScheduledRenewal> LoadRenewals()
        {
            var result = new List<ScheduledRenewal>();
            var values = Registry.GetValue(registryKey, renewalsValueName, null) as string[];
            if (values != null)
            {
                foreach (var renewal in values)
                {
                    result.Add(ScheduledRenewal.Load(renewal));
                }
            }
            return result;
        }

        public void SaveRenewals(List<ScheduledRenewal> renewals)
        {
            var serialized = new List<string>();

            foreach (var renewal in renewals)
            {
                serialized.Add(renewal.Save());
            }

            Registry.SetValue(registryKey, renewalsValueName, serialized.ToArray());
        }
    }
}