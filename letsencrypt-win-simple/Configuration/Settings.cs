using System.Collections.Generic;
using Microsoft.Win32;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple
{
    public class Settings
    {
        public const int maxNames = 100;
        private string _registryHome;
        private string _configPath;
        private LogService _log;
        private const string _renewalsKey = "Renewals";

        public Settings(LogService log, string clientName, string configPath, string cleanBaseUri)
        {
            _log = log;
            _configPath = configPath;
            
            var key = $"\\Software\\{clientName}\\{cleanBaseUri}";
            _registryHome = $"HKEY_CURRENT_USER{key}";
            if (RenewalStore == null)
            {
                _registryHome = $"HKEY_LOCAL_MACHINE{key}";
            }
            _log.Verbose("Using registry key {_registryHome}", _registryHome);
            _log.Verbose("Settings {@settings}", this);
        }

        internal string[] RenewalStore
        {
            get { return Registry.GetValue(_registryHome, _renewalsKey, null) as string[]; }
            set { Registry.SetValue(_registryHome, _renewalsKey, value); }
        }

        internal int HostsPerPage()
        {
            int hostsPerPage = 50;
            try {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
            } catch (Exception ex) {
                _log.Error(ex, "Error getting HostsPerPage setting, setting to default value");
            }
            return hostsPerPage;
        }
    }
}