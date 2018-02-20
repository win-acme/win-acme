using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Services.RenewalStore
{
    class RegistryRenewalStore : BaseRenewalStore
    {
        private const string _renewalsKey = "Renewals";
        private string _registryHome;

        public RegistryRenewalStore(ILogService log, IOptionsService options, SettingsService settings) : base(log, options, settings)
        {
            var key = $"\\Software\\{_settings.ClientName}\\{options.Options.BaseUri}";
            _registryHome = $"HKEY_CURRENT_USER{key}";
            if (RenewalStore == null)
            {
                _registryHome = $"HKEY_LOCAL_MACHINE{key}";
            }
            _log.Verbose("Using registry key {_registryHome}", _registryHome);
        }

        public override string[] RenewalStore
        {
            get
            {
                return Registry.GetValue(_registryHome, _renewalsKey, null) as string[];
            }
            set
            {
                Registry.SetValue(_registryHome, _renewalsKey, value);
            }
        }
    }
}
