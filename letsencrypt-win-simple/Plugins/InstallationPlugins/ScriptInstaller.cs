using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class ScriptInstallerFactory : BaseInstallationPluginFactory<IISInstaller>
    {
        public const string PluginName = "Manual";
        public ScriptInstallerFactory() : base(PluginName, "Run external script") { }
    }

    class ScriptInstaller : ScriptClient, IInstallationPlugin
    {
        private ScheduledRenewal _renewal;

        public ScriptInstaller(ScheduledRenewal renewal, ILogService logService) : base(logService)
        {
            _renewal = renewal;
        }

        void IInstallationPlugin.Aquire() { }
        void IInstallationPlugin.Default() { }
        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _renewal.Script,
                  _renewal.ScriptParameters,
                  _renewal.Binding.Host,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  _renewal.CentralSslStore);
        }
    }
}