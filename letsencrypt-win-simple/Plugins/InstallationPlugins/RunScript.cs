using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;
using System;
using Autofac;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class RunScript : ScriptClient, IInstallationPluginFactory, IInstallationPlugin
    {
        public const string PluginName = "Manual";
        public string Name => PluginName;
        public string Description => "Run external script";
        public IInstallationPlugin Instance(ILifetimeScope scope)
        {
            return this;
        }
        public void Aquire(IOptionsService options, IInputService input, ScheduledRenewal target) { }
        public bool CanInstall(ScheduledRenewal renewal) => true;
        public IInstallationPluginFactory CreateInstance(ScheduledRenewal target) => this;
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