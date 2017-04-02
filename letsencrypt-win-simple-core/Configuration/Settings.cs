using System.Collections.Generic;
using LetsEncrypt.ACME.Simple.Core.Schedules;
using Microsoft.Win32;

namespace LetsEncrypt.ACME.Simple.Core.Configuration
{
    public class Settings
    {
        public string ScheduledTaskName
        {
            get => Registry.GetValue(_registryKey, "ScheduledTaskName", null) as string;
            set => Registry.SetValue(_registryKey, "ScheduledTaskName", value);
        }

        readonly string _registryKey;

        public Settings(string clientName, string cleanBaseUri)
        {
            _registryKey = $"HKEY_CURRENT_USER\\Software\\{clientName}\\{cleanBaseUri}";
        }

        private const string RenewalsValueName = "Renewals";

        public List<ScheduledRenewal> LoadRenewals()
        {
            var result = new List<ScheduledRenewal>();
            var values = Registry.GetValue(_registryKey, RenewalsValueName, null) as string[];
            if (values == null)
                return result;

            foreach (var renewal in values)
                result.Add(ScheduledRenewal.Load(renewal));

            return result;
        }

        public void SaveRenewals(List<ScheduledRenewal> renewals)
        {
            var serialized = new List<string>();

            foreach (var renewal in renewals)
                serialized.Add(renewal.Save());

            Registry.SetValue(_registryKey, RenewalsValueName, serialized.ToArray());
        }
    }
}