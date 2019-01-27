using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpOptionsFactory : InstallationPluginFactory<IISFtp, IISFtpOptions>
    {
        private IIISClient _iisClient;

        public IISFtpOptionsFactory(ILogService log, IIISClient iisClient) : base(log)
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall() => _iisClient.HasFtpSites;
        public override IISFtpOptions Aquire(Target renewal, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                false);
            ret.SiteId = chosen;
            return ret;
        }

        public override IISFtpOptions Default(Target renewal, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<IISFtpArguments>();
            var ret = new IISFtpOptions();
            var siteId = args.FtpSiteId;
            if (siteId == null)
            {
                throw new Exception($"Missing parameter --{nameof(args.FtpSiteId).ToLower()}");
            }
            // Throws exception when site is not found
            var site = _iisClient.GetFtpSite(siteId.Value);
            ret.SiteId = site.Id;
            return ret;
        }
    }
}
