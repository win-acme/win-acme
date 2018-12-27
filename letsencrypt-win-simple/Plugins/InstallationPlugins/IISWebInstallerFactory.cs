using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebInstallerFactory : BaseInstallationPluginFactory<IISWebInstaller, IISWebInstallerOptions>
    {
        public const string PluginName = "IIS";
        private IISClient _iisClient;
        public IISWebInstallerFactory(ILogService log, IISClient iisClient) : base(log, PluginName, "Create or update https bindings in IIS")
        {
            _iisClient = iisClient;
        }
        public override bool CanInstall() => _iisClient.HasWebSites;
        public override IISWebInstallerOptions Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
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
                renewal.Target.InstallationSiteId = chosen;
            }
            return null;
        }

        public override IISWebInstallerOptions Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            var installationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId);
            if (installationSiteId != null)
            {
                var site = _iisClient.GetWebSite(installationSiteId.Value); // Throws exception when not found
                renewal.Target.InstallationSiteId = site.Id;
            }
            else if (renewal.Target.TargetSiteId == null)
            {
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
            return null;
        }
    }
}
