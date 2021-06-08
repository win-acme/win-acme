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
    internal class IISWebOptionsFactory : InstallationPluginFactory<IISWeb, IISWebOptions>
    {
        public override int Order => 5;
        private readonly IIISClient _iisClient;
        private readonly ArgumentsInputService _arguments;

        public IISWebOptionsFactory(IIISClient iisClient, ArgumentsInputService arguments, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IISWeb.Disabled(userRoleService, iisClient);
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) => 
            storeTypes.Contains(typeof(CertificateStore)) || 
            storeTypes.Contains(typeof(CentralSsl));

        private ArgumentResult<int?> NewBindingPort => _arguments.
            GetInt<IISWebArguments>(x => x.SSLPort).
            WithDefault(IISClient.DefaultBindingPort).
            DefaultAsNull().
            Validate(x => Task.FromResult(x >= 1), "invalid port").
            Validate(x => Task.FromResult(x <= 65535), "invalid port");

        private ArgumentResult<string?> NewBindingIp => _arguments.
            GetString<IISWebArguments>(x => x.SSLIPAddress).
            WithDefault(IISClient.DefaultBindingIp).
            DefaultAsNull().
            Validate(x => Task.FromResult(x == "*" || IPAddress.Parse(x!) != null), "invalid address");

        private ArgumentResult<long?> InstallationSite => _arguments.
            GetLong<IISWebArguments>(x => x.InstallationSiteId).
            Validate(x => Task.FromResult(_iisClient.GetWebSite(x!.Value) != null), "invalid site");

        public override async Task<IISWebOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISWebOptions()
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
                   _iisClient.WebSites,
                   x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override async Task<IISWebOptions> Default(Target target)
        {
            var ret = new IISWebOptions()
            {
                NewBindingPort = await NewBindingPort.GetValue(),
                NewBindingIp = await NewBindingIp.GetValue(),
                SiteId = await InstallationSite.Required(!target.IIS).GetValue()
            };
            return ret;
        }
    }
}
