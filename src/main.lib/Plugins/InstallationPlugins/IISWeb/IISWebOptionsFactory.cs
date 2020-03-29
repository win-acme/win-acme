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
    internal class IISWebOptionsFactory : InstallationPluginFactory<IISWeb, IISWebOptions>
    {
        public override int Order => 5;
        private readonly IIISClient _iisClient;
        private readonly IArgumentsService _arguments;

        public IISWebOptionsFactory(IIISClient iisClient, IArgumentsService arguments, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _arguments = arguments;
            Disabled = IISWeb.Disabled(userRoleService, iisClient);
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes) => storeTypes.Contains(typeof(CertificateStore)) || storeTypes.Contains(typeof(CentralSsl));

        public override async Task<IISWebOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<IISWebArguments>();
            var ret = new IISWebOptions(args);
            var ask = true;
            if (target.IIS)
            {
                if (runLevel.HasFlag(RunLevel.Advanced))
                {
                    ask = await inputService.PromptYesNo("Use different site for installation?", false);
                }
                else
                {
                    ask = false;
                }
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

        public override Task<IISWebOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<IISWebArguments>();
            var ret = new IISWebOptions(args);
            if (args.InstallationSiteId != null)
            {
                // Throws exception when not found
                var site = _iisClient.GetWebSite(args.InstallationSiteId.Value);
                ret.SiteId = site.Id;
            }
            else if (!target.IIS)
            {
                throw new Exception($"Missing parameter --{nameof(args.InstallationSiteId).ToLower()}");
            }
            return Task.FromResult(ret);
        }
    }
}
