using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class IISInstallerFactory : BaseInstallationPluginFactory<IISInstaller>
    {
        public const string PluginName = "IIS";
        public IISInstallerFactory() : base(PluginName, "Create or update IIS bindings") { }
        public override bool CanInstall(ScheduledRenewal target) => IISClient.Version.Major > 0;
    }

    class IISInstaller : IISClient, IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private ITargetPlugin _targetPlugin;

        public IISInstaller(ScheduledRenewal renewal, ITargetPlugin targetPlugin) : base()
        {
            _renewal = renewal;
            _targetPlugin = targetPlugin;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            SSLFlags flags = 0;
            if (Version.Major >= 8)
            {
                flags |= SSLFlags.SNI;
            }
            if (newCertificate.Store == null)
            {
                if (Version.Major < 8)
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
                AddOrUpdateBindings(split, flags, newCertificate, oldCertificate);
            }
        }
        void IInstallationPlugin.Aquire() { }
        void IInstallationPlugin.Default() { }
    }
}
