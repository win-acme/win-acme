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
    internal class IISWebOptionsFactory : InstallationPluginFactory<IISWeb, IISWebOptions>
    {
        private IIISClient _iisClient;
        public IISWebOptionsFactory(ILogService log, IIISClient iisClient) : base(log)
        {
            _iisClient = iisClient;
        }

        public override bool CanInstall(IEnumerable<Type> storeTypes)
        {
            return _iisClient.HasWebSites && 
                (storeTypes.Contains(typeof(CertificateStore)) ||
                 storeTypes.Contains(typeof(CentralSsl)));
        }

        public override IISWebOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var args = arguments.GetArguments<IISWebArguments>();
            var ret = new IISWebOptions(args);
            var ask = true;
            if (target.IIS)
            {
                if (runLevel.HasFlag(RunLevel.Advanced))
                {
                    ask = inputService.PromptYesNo("Use different site for installation?", false);
                }
                else
                {
                    ask = false;
                }
            }
            if (ask)
            {
                var chosen = inputService.ChooseFromList("Choose site to create new bindings",
                   _iisClient.WebSites,
                   x => Choice.Create(x.Id, x.Name, x.Id.ToString()));
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override IISWebOptions Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<IISWebArguments>();
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
            return ret;
        }
    }
}
