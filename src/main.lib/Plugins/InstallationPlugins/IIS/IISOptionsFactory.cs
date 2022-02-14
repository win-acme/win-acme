using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISOptionsFactory : InstallationPluginFactory<IIS, IISOptions>
    {
        public override int Order => 5;
        private readonly IIISClient _iisClient;
        private readonly ArgumentsInputService _arguments;

        /// <summary>
        /// Match with the legacy target plugin names
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override bool Match(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "iisftp" => true,
                _ => base.Match(name),
            };
        }

        public IISOptionsFactory(IIISClient iisClient, ArgumentsInputService arguments, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IIS.Disabled(userRoleService, iisClient);
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) => 
            storeTypes.Contains(typeof(CertificateStore)) || 
            storeTypes.Contains(typeof(CentralSsl));

        private ArgumentResult<int?> NewBindingPort => _arguments.
            GetInt<IISArguments>(x => x.SSLPort).
            WithDefault(IISClient.DefaultBindingPort).
            DefaultAsNull().
            Validate(x => Task.FromResult(x >= 1), "invalid port").
            Validate(x => Task.FromResult(x <= 65535), "invalid port");

        private ArgumentResult<string?> NewBindingIp => _arguments.
            GetString<IISArguments>(x => x.SSLIPAddress).
            WithDefault(IISClient.DefaultBindingIp).
            DefaultAsNull().
            Validate(x => Task.FromResult(x == "*" || IPAddress.Parse(x!) != null), "invalid address");

        private ArgumentResult<long?> InstallationSite => _arguments.
            GetLong<IISArguments>(x => x.InstallationSiteId).
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value) != null), "invalid site");

        private ArgumentResult<long?> FtpSite => _arguments.
            GetLong<IISArguments>(x => x.FtpSiteId).
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value) != null), "invalid site").
            Validate(x => Task.FromResult(_iisClient.GetSite(x!.Value).Type == IISSiteType.Ftp), "not an ftp site");

        public override async Task<IISOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISOptions()
            {
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue()
            };
            var ask = true;
            if (target.IIS)
            {
                ask = runLevel.HasFlag(RunLevel.Advanced) && 
                    await inputService.PromptYesNo("Use different site for installation?", false);
            }
            if (ask)
            {
                var chosen = await inputService.ChooseRequired("Choose site to create new bindings",
                   _iisClient.Sites,
                   x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override async Task<IISOptions> Default(Target target)
        {
            var siteId = await FtpSite.GetValue();
            if (siteId == null)
            {
                siteId = await InstallationSite.Required(!target.IIS).GetValue();
            }
            var ret = new IISOptions()
            {
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue(),
                SiteId = siteId
            };
            return ret;
        }
    }
}
