using System.Collections.Generic;
using Microsoft.Win32;
using System.Linq;
using System;

namespace LetsEncrypt.ACME.Simple
{
    public class Settings
    {
        public const int maxNames = 100;
        private string _registryHome;
        private const string _renewalsKey = "Renewals";

        public Settings(string clientName, string cleanBaseUri)
        {
            var key = $"\\Software\\{clientName}\\{cleanBaseUri}";
            _registryHome = $"HKEY_CURRENT_USER{key}";
            if (_renewalStore == null)
            {
                _registryHome = $"HKEY_LOCAL_MACHINE{key}";
            }
            Program.Log.Verbose("Using registry key {_registryHome}", _registryHome);
        }

        private string[] _renewalStore
        {
            get { return Registry.GetValue(_registryHome, _renewalsKey, null) as string[]; }
            set { Registry.SetValue(_registryHome, _renewalsKey, value); }
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get {
                if (_renewalsCache == null) {
                    _renewalsCache = _renewalStore.Select(x => ScheduledRenewal.Load(x)).Where(x => x != null).ToList();
                }
                return _renewalsCache;
            }
            set {
                _renewalsCache = value.ToList();
                _renewalStore = _renewalsCache.Select(x => x.Save()).ToArray();
            }
        }
        private List<ScheduledRenewal>  _renewalsCache = null;

        internal int HostsPerPage()
        {
            int hostsPerPage = 50;
            try
            {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
            }
            catch (Exception ex)
            {
                Program.Log.Error("Error getting HostsPerPage setting, setting to default value. Error: {@ex}", ex);
            }
            return hostsPerPage;
        }

    }
}