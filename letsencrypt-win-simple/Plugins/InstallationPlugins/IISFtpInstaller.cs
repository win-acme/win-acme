using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpInstallerFactory : BaseInstallationPluginFactory<IISFtpInstaller>
    {
        private IISClient _iisClient;

        public IISFtpInstallerFactory(ILogService log, IISClient iisClient) : 
            base(log, "iisftp", "Create or update ftps bindings in IIS")
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall(ScheduledRenewal renewal) => _iisClient.Version.Major > 8;
        public override void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                false);
                renewal.Binding.InstallationSiteId = chosen;
        }

        public override void Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            var installationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId);
            if (installationSiteId != null)
            {
                var site = _iisClient.GetFtpSite(installationSiteId.Value); // Throws exception when not found
                renewal.Binding.InstallationSiteId = site.Id;
            }
            else
            {
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
        }
    }

    class IISFtpInstaller : IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private IISClient _iisClient;

        public IISFtpInstaller(ScheduledRenewal renewal, IISClient iisClient)
        {
            _iisClient = iisClient;
            _renewal = renewal;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            _iisClient.UpdateFtpSite(_renewal.Binding, SSLFlags.None, newCertificate, oldCertificate);
        }
    }
}
