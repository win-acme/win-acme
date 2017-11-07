using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;
using Autofac;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class ScriptInstallerFactory : IInstallationPluginFactory
    {
        public const string PluginName = "Manual";
        public string Name => PluginName;
        public string Description => "Run external script";
        public Type Instance => typeof(ScriptInstaller);
        public void Aquire(IOptionsService options, IInputService input, ScheduledRenewal target) { }
        public bool CanInstall(ScheduledRenewal renewal) => true;
        public void Default(IOptionsService options, ScheduledRenewal target) { }
    }

    class ScriptInstaller : ScriptClient, IInstallationPlugin
    {
        private ScheduledRenewal _renewal;

        public ScriptInstaller(ScheduledRenewal renewal) : base()
        {
            _renewal = renewal;
        }

        public void Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
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
                  _optionsService.Options.CentralSslStore);
        }
    }
}