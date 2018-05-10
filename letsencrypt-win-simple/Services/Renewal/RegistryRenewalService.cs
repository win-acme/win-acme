using Microsoft.Win32;
using System.Linq;

namespace PKISharp.WACS.Services.Renewal
{
    internal class RegistryRenewalService : BaseRenewalService
    {
        private const string _renewalsKey = "Renewals";
        private string _hive;
        private string _clientName;
 
        public RegistryRenewalService(
            ILogService log,
            IOptionsService options, 
            SettingsService settings, 
            string hive) : base(settings, options, log)
        {
            _clientName = settings.ClientNames.Last();
            _hive = $"HKEY_CURRENT_USER{Key}";
            if (ReadRenewalsRaw() == null)
            {
                _hive = $"HKEY_LOCAL_MACHINE{Key}";
            }
            _log.Verbose("Store renewals in registry {_registryHome}", _hive);
        }

        private string Key => $"\\Software\\{_clientName}\\{_baseUri}";

        internal override string[] ReadRenewalsRaw()
        {
            return Registry.GetValue(_hive, _renewalsKey, null) as string[];
        }

        internal override void WriteRenewalsRaw(string[] Renewals)
        {
            Registry.SetValue(_hive, _renewalsKey, Renewals);
        }
    }
}
