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
        private readonly ArgumentsInputService _arguments;

        public override int Order => 10;

        public IISFtpOptionsFactory(IIISClient iisClient, ArgumentsInputService arguments, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IISFtp.Disabled(userRoleService, iisClient);
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) => storeTypes.Contains(typeof(CertificateStore));

        public override async Task<IISFtpOptions> Aquire(Target renewal, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISFtpOptions();
            var chosen = await inputService.ChooseRequired("Choose ftp site to bind the certificate to",
                _iisClient.FtpSites,
                x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
            ret.SiteId = chosen;
            return ret;
        }

        public override async Task<IISFtpOptions> Default(Target renewal)
        {
            return new IISFtpOptions()
            {
                SiteId = (long)await _arguments.
                    GetLong<IISFtpArguments>(x => x.FtpSiteId).
                    Required().
                    Validate(x => Task.FromResult(_iisClient.GetFtpSite(x!.Value) != null), "invalid site").
                    GetValue()
            };
        }
    }
}
