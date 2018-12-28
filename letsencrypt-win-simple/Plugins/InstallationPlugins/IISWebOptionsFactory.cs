using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebOptionsFactory : BaseInstallationPluginFactory<IISWeb, IISWebOptions>
    {
        private IISClient _iisClient;
        public IISWebOptionsFactory(ILogService log, IISClient iisClient) : base(log, "IIS", "Create or update https bindings in IIS")
        {
            _iisClient = iisClient;
        }
        public override bool CanInstall() => _iisClient.HasWebSites;
        public override IISWebOptions Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISWebOptions(optionsService.Options);
            var ask = true;
            if (renewal.Target.IIS)
            {
                if (runLevel == RunLevel.Advanced)
                {
                    ask = inputService.PromptYesNo("Use different site for installation?");
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
                   x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                   false);
                ret.SiteId = chosen;
            }
            return ret;
        }

        public override IISWebOptions Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            var ret = new IISWebOptions(optionsService.Options);
            var installationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId);
            if (installationSiteId != null)
            {
                var site = _iisClient.GetWebSite(installationSiteId.Value); // Throws exception when not found
                ret.SiteId = site.Id;
            }
            else if (!renewal.Target.IIS)
            {
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
            return ret;
        }
    }
}
