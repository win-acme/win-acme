using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class Script : ScriptClient, IInstallationPlugin
    {
        public const string PluginName = "Manual";
        public string Name => PluginName;
        public string Description => "Run external script";
        public void Aquire(IOptionsService options, IInputService input, Target target) { }
        public bool CanInstall(Target target) => true;
        public IInstallationPlugin CreateInstance(Target target) => this;
        public void Default(IOptionsService options, Target target) { }

        public void Install(Target target, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _optionsService.Options.Script,
                  _optionsService.Options.ScriptParameters,
                  target.Host,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  _optionsService.Options.CentralSslStore);
        }
    }
}