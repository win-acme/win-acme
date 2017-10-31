using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class RunScript : ScriptClient, IInstallationPlugin
    {
        public const string PluginName = "Manual";
        public string Name => PluginName;
        public string Description => "Run external script";
        public void Aquire(IOptionsService options, IInputService input, ScheduledRenewal target) { }
        public bool CanInstall(ScheduledRenewal renewal) => true;
        public IInstallationPlugin CreateInstance(ScheduledRenewal target) => this;
        public void Default(IOptionsService options, ScheduledRenewal target) { }

        public void Install(ScheduledRenewal renewal, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  renewal.Script,
                  renewal.ScriptParameters,
                  renewal.Binding.Host,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  _optionsService.Options.CentralSslStore);
        }
    }
}