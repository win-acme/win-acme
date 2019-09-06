using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpOptionsFactory : InstallationPluginFactory<IISFtp, IISFtpOptions>
    {
        private readonly IIISClient _iisClient;
        private IArgumentsService _arguments;

        public override int Order => 10;

        public IISFtpOptionsFactory(IIISClient iisClient, IArgumentsService arguments)
        {
            _iisClient = iisClient;
            _arguments = arguments;
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes)
        {
            return _iisClient.HasFtpSites && storeTypes.Contains(typeof(CertificateStore));
        }

        public override IISFtpOptions Aquire(Target renewal, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
            ret.SiteId = chosen;
            return ret;
        }

        public override IISFtpOptions Default(Target renewal)
        {
            var args = _arguments.GetArguments<IISFtpArguments>();
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
