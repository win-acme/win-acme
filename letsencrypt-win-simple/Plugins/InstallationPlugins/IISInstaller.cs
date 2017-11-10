using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Linq;
using static LetsEncrypt.ACME.Simple.Clients.IISClient;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class IISInstallerFactory : BaseInstallationPluginFactory<IISInstaller>
    {
        public const string PluginName = "IIS";
        public IISInstallerFactory() : base(PluginName, "Create or update IIS bindings") { }
        public override bool CanInstall(ScheduledRenewal target) => IISClient.Version.Major > 0;
    }

    class IISInstaller : IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private ILogService _log;
        private ITargetPlugin _targetPlugin;
        private IISClient _iisClient;

        public IISInstaller(ScheduledRenewal renewal, IISClient iisClient, ITargetPlugin targetPlugin, ILogService log) 
        {
            _iisClient = iisClient;
            _renewal = renewal;
            _targetPlugin = targetPlugin;
            _log = log;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            SSLFlags flags = 0;
            if (IISClient.Version.Major >= 8)
            {
                flags |= SSLFlags.SNI;
            }
            if (newCertificate.Store == null)
            {
                if (IISClient.Version.Major < 8)
                {
                    var errorMessage = "Centralized SSL is only supported on IIS8+";
                    _log.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    flags |= SSLFlags.CentralSSL;
                }
            }
            foreach (var split in _targetPlugin.Split(_renewal.Binding))
            {
                _iisClient.AddOrUpdateBindings(split, flags, newCertificate, oldCertificate);
            }
        }

        void IInstallationPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            if (_renewal.Binding.IIS == true && _renewal.Binding.SiteId > 0)
            {
                // No need to ask anything
            }
            else
            {
                var chosen = inputService.ChooseFromList("Choose site to install certificate",
                    _iisClient.RunningWebsites(),
                    x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                    false);
                _renewal.Binding.SiteId = chosen;
            }
        }

        void IInstallationPlugin.Default(IOptionsService optionsService)
        {
            var rawSiteId = optionsService.TryGetRequiredOption(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId);
            long siteId = 0;
            if (long.TryParse(rawSiteId, out siteId))
            {
                var found = _iisClient.RunningWebsites().FirstOrDefault(site => site.Id == siteId);
                if (found != null)
                {
                    _renewal.Binding.SiteId = found.Id;
                }
                else
                {
                    throw new Exception($"Unable to find SiteId {siteId}");
                }
            }
            else
            {
                throw new Exception($"Invalid SiteId {optionsService.Options.SiteId}");
            }
        }
    }
}
