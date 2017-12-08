using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.Base;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;
using System;
using static LetsEncrypt.ACME.Simple.Clients.IISClient;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class IISInstallerFactory : BaseInstallationPluginFactory<IISInstaller>
    {
        public const string PluginName = "IIS";
        private IISClient _iisClient;
        public IISInstallerFactory(ILogService log, IISClient iisClient) : base(log, PluginName, "Create or update IIS bindings")
        {
            _iisClient = iisClient;
        }
        public override bool CanInstall(ScheduledRenewal renewal) => _iisClient.Version.Major > 0;
        public override void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService)
        {
            var ask = true;
            if (renewal.Binding.IIS == true)
            {
                ask = inputService.PromptYesNo("Use different site for installation?");
            }
            if (ask)
            {
                var chosen = inputService.ChooseFromList("Choose site to create new bindings",
                   _iisClient.RunningWebsites(),
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
                var site = _iisClient.GetSite(installationSiteId.Value); // Throws exception when not found
                renewal.Binding.InstallationSiteId = site.Id;
            }
            else if (renewal.Binding.TargetSiteId == null)
            {
                throw new Exception($"Missing parameter --{nameof(optionsService.Options.InstallationSiteId).ToLower()}");
            }
        }
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
