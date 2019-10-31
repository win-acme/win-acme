using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISFtpOptionsFactory : InstallationPluginFactory<IISFtp, IISFtpOptions>
    {
        private readonly IIISClient _iisClient;
        private readonly IArgumentsService _arguments;

        public override int Order => 10;

        public IISFtpOptionsFactory(IIISClient iisClient, IArgumentsService arguments, UserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IISFtp.Disabled(userRoleService, iisClient);
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) => storeTypes.Contains(typeof(CertificateStore));

        public override async Task<IISFtpOptions> Aquire(Target renewal, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = await inputService.ChooseFromList("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
            ret.SiteId = chosen;
            return ret;
        }

        public override Task<IISFtpOptions> Default(Target renewal)
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
            return Task.FromResult(ret);
        }
    }
}
