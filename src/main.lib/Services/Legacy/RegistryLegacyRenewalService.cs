using Microsoft.Win32;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Host.Services.Legacy;
using System;

namespace PKISharp.WACS.Services.Legacy
{
    internal class RegistryLegacyRenewalService : BaseLegacyRenewalService
    {
        private const string _renewalsKey = "Renewals";
        private readonly string _hive;
        private readonly string _clientName = "letsencrypt-win-simple";
        private readonly string _baseUri;

        public RegistryLegacyRenewalService(
            ILogService log,
            MainArguments main,
            LegacySettingsService settings) :
            base(settings, log)
        {
            if (main.BaseUri == null)
            {
                throw new InvalidOperationException("Missing main.BaseUri");
            }
            _baseUri = main.BaseUri;
            _hive = $"HKEY_CURRENT_USER{Key}";
            if (Registry.GetValue(_hive, _renewalsKey, null) == null)
            {
                _hive = $"HKEY_LOCAL_MACHINE{Key}";
            }
            _log.Debug("Read legacy renewals from registry {_registryHome}", _hive);
        }

        private string Key => $"\\Software\\{_clientName}\\{_baseUri}";

        internal override string[] RenewalsRaw => Registry.GetValue(_hive, _renewalsKey, null) as string[] ?? new string[] { };
    }
}
