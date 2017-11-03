using System;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;
using Autofac;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class IISInstallerFactory : IInstallationPluginFactory
    {
        public const string PluginName = "IIS";
        public string Name => PluginName;
        public string Description => "Create or update IIS bindings";
        public IInstallationPlugin Instance(ILifetimeScope scope) => scope.Resolve<IISInstaller>();
        public void Aquire(IOptionsService options, IInputService input, ScheduledRenewal target) { }
        public bool CanInstall(ScheduledRenewal target) => IISClient.Version.Major > 0;
        public void Default(IOptionsService options, ScheduledRenewal target) { }
    }

    class IISInstaller : IISClient, IInstallationPlugin
    {
        public void Install(ScheduledRenewal renewal, CertificateInfo newCertificate, CertificateInfo oldCertificate)
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
            AddOrUpdateBindings(renewal.Binding, flags, newCertificate, oldCertificate);
        }
    }
}
