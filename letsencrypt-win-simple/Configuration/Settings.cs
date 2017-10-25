using Microsoft.Win32;
using System;
using LetsEncrypt.ACME.Simple.Services;
using Autofac;

namespace LetsEncrypt.ACME.Simple
{
    public class Settings
    {
        public const int maxNames = 100;
        private string _registryHome;
        private string _configPath;
        private ILogService _log;
        private const string _renewalsKey = "Renewals";

        public Settings(string clientName, string configPath, string cleanBaseUri)
        {
            _log = Program.Container.Resolve<ILogService>();
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