using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebInstallerFactory : BaseInstallationPluginFactory<IISWebInstaller>
    {
        public const string PluginName = "IIS";
        private IISClient _iisClient;
        public IISWebInstallerFactory(ILogService log, IISClient iisClient) : base(log, PluginName, "Create or update https bindings in IIS")
        {
            _iisClient = iisClient;
        }
        public override bool CanInstall(ScheduledRenewal renewal) => _iisClient.HasWebSites;
        public override void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ask = true;
            if (renewal.Binding.IIS == true)
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
                renewal.Binding.InstallationSiteId = chosen;
            }
        }

        public override void Default(ScheduledRenewal renewal, IOptionsService optionsService)
        {
            var installationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId);
            if (installationSiteId != null)
            {
                var site = _iisClient.GetWebSite(installationSiteId.Value); // Throws exception when not found
                renewal.Binding.InstallationSiteId = site.Id;
            }
            else if (renewal.Binding.TargetSiteId == null)
            {
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
        }
    }

    internal class IISWebInstaller : IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private ILogService _log;
        private ITargetPlugin _targetPlugin;
        private IISClient _iisClient;

        public IISWebInstaller(ScheduledRenewal renewal, IISClient iisClient, ITargetPlugin targetPlugin, ILogService log) 
        {
            _iisClient = iisClient;
            _renewal = renewal;
            _targetPlugin = targetPlugin;
            _log = log;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            SSLFlags flags = 0;
            if (newCertificate.Store == null)
            {
                if (_iisClient.Version.Major < 8)
                {
                    var errorMessage = "Centralized SSL is only supported on IIS8+";
                    _log.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    flags |= SSLFlags.CentralSSL;
                }
            }
            foreach (var split in _targetPlugin.Split(_renewal.Binding))
            {
                _iisClient.AddOrUpdateBindings(split, flags, newCertificate, oldCertificate);
            }
        }
    }
}
