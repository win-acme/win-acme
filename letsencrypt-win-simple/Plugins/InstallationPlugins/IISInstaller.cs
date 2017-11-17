using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Linq;
using static LetsEncrypt.ACME.Simple.Clients.IISClient;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class IISInstallerFactory : BaseInstallationPluginFactory<IISInstaller>
    {
        public const string PluginName = "IIS";
        public IISInstallerFactory() : base(PluginName, "Create or update IIS bindings") { }
        public override bool CanInstall(ScheduledRenewal target) => IISClient.Version.Major > 0;
    }

    class IISInstaller : IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private ILogService _log;
        private ITargetPlugin _targetPlugin;
        private IISClient _iisClient;

        public IISInstaller(ScheduledRenewal renewal, IISClient iisClient, ITargetPlugin targetPlugin, ILogService log) 
        {
            _iisClient = iisClient;
            _renewal = renewal;
            _targetPlugin = targetPlugin;
            _log = log;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            SSLFlags flags = 0;
            if (IISClient.Version.Major >= 8)
            {
                flags |= SSLFlags.SNI;
            }
            if (newCertificate.Store == null)
            {
                if (IISClient.Version.Major < 8)
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

        void IInstallationPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            var ask = true;
            if (_renewal.Binding.IIS == true)
            {
                ask = inputService.PromptYesNo("Use different site for installation?");
            }
            if (ask)
            {
                var chosen = inputService.ChooseFromList("Choose site to create new bindings",
                   _iisClient.RunningWebsites(),
                   x => new Choice<long>(x.Id) { Description = x.Name, Command = x.Id.ToString() },
                   false);
                _renewal.Binding.InstallationSiteId = chosen;
            }
        }

        void IInstallationPlugin.Default(IOptionsService optionsService)
        {
            // IIS 
            var installationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.InstallationSiteId), optionsService.Options.InstallationSiteId);
            if (installationSiteId != null)
            {
                var site = _iisClient.GetSite(installationSiteId.Value); // Throws exception when not found
                _renewal.Binding.InstallationSiteId = site.Id;
            }
            else if (_renewal.Binding.TargetSiteId == null)
            {
                _log.Error("Missing parameter --{p}", nameof(optionsService.Options.InstallationSiteId).ToLower());
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
        }
    }
}
