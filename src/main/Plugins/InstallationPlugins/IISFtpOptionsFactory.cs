using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpOptionsFactory : InstallationPluginFactory<IISFtp, IISFtpOptions>
    {
        private IISClient _iisClient;

        public IISFtpOptionsFactory(ILogService log, IISClient iisClient) : base(log)
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall() => _iisClient.HasFtpSites;
        public override IISFtpOptions Aquire(Target renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                false);
            ret.SiteId = chosen;
            return ret;
        }

        public override IISFtpOptions Default(Target renewal, IOptionsService optionsService)
        {
            var ret = new IISFtpOptions();
            var siteId = optionsService.TryGetLong(nameof(optionsService.Options.FtpSiteId), optionsService.Options.FtpSiteId) ??
                         optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId) ??
                         optionsService.TryGetLong(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId) ??
                         throw new Exception($"Missing parameter --{nameof(optionsService.Options.FtpSiteId).ToLower()}");
            var site = _iisClient.GetFtpSite(siteId);
            ret.SiteId = site.Id;
            return ret;
        }
    }
}
