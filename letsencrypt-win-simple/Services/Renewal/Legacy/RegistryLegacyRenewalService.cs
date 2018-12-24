using Microsoft.Win32;
using System.Linq;

namespace PKISharp.WACS.Services.Renewal.Legacy
{
    internal class RegistryLegacyRenewalService : BaseLegacyRenewalService
    {
        private const string _renewalsKey = "Renewals";
        private readonly string _hive;
        private readonly string _clientName;
        private readonly string _baseUri;
 
        public RegistryLegacyRenewalService(
            ILogService log,
            IOptionsService options, 
            SettingsService settings, 
            string hive) : base(settings, options, log)
        {
            _baseUri = options.Options.BaseUri;
            _clientName = settings.ClientNames.Last();
            _hive = $"HKEY_CURRENT_USER{Key}";
            if (RenewalsRaw == null)
            {
                _hive = $"HKEY_LOCAL_MACHINE{Key}";
            }
            _log.Verbose("Store renewals in registry {_registryHome}", _hive);
        }

        private string Key => $"\\Software\\{_clientName}\\{_baseUri}";

        internal override string[] RenewalsRaw
        {
            get
            {
                return Registry.GetValue(_hive, _renewalsKey, null) as string[];
            }
        }
    }
}
