using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LetsEncrypt.ACME.Simple
{
    public class Settings
    {
        public bool AgreedToTOS { get; set; }
        public string ContactEmail { get; set; }

        string registryKey;

        public Settings(string cleanBaseUri)
        {
            registryKey = "HKEY_CURRENT_USER\\Software\\Let's Encrypt\\" + cleanBaseUri;
        }

        public List<ScheduledRenewal> LoadRenewals()
        {
            var result = new List<ScheduledRenewal>();
            var values = Registry.GetValue(registryKey, "Renewals", null) as string[];
            foreach (var renewal in values)
            {
                //result.Add(ScheduledRenewal.Load(renewal));
            }
            return result;
        }
    }
}
