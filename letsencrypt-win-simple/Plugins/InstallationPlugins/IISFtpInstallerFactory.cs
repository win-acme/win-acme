using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpInstallerFactory : BaseInstallationPluginFactory<IISFtpInstaller, IISFtpInstallerOptions>
    {
        private IISClient _iisClient;

        public IISFtpInstallerFactory(ILogService log, IISClient iisClient) :
            base(log, "iisftp", "Create or update ftps bindings in IIS")
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall() => _iisClient.HasFtpSites;
        public override IISFtpInstallerOptions Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                false);
            renewal.Target.FtpSiteId = chosen;
            return null;
        }

        public override IISFtpInstallerOptions Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            var siteId = optionsService.TryGetLong(nameof(optionsService.Options.FtpSiteId), optionsService.Options.FtpSiteId) ??
                         optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId) ??
                         optionsService.TryGetLong(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId) ??
                         throw new Exception($"Missing parameter --{nameof(optionsService.Options.FtpSiteId).ToLower()}");
            var site = _iisClient.GetFtpSite(siteId);
            renewal.Target.FtpSiteId = site.Id;
            return null;
        }
    }
}
