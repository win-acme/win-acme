using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins
{
    class ScriptInstallerFactory : BaseInstallationPluginFactory<ScriptInstaller>
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

        void IInstallationPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            inputService.Show("Full instructions", "https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/Install-Script");
            do
            {
                _renewal.Script = optionsService.TryGetOption(optionsService.Options.Script, inputService, "Enter the path to the script that you want to run after renewal");
            }
            while (!_renewal.Script.ValidFile(_log));
            inputService.Show("{0}", "Hostname");
            inputService.Show("{1}", ".pfx password");
            inputService.Show("{2}", ".pfx path");
            inputService.Show("{3}", "Certificate store name");
            inputService.Show("{4}", "Certificate friendly name");
            inputService.Show("{5}", "Certificate thumbprint");
            inputService.Show("{6}", "Central SSL store path");
            _renewal.ScriptParameters = optionsService.TryGetOption(optionsService.Options.ScriptParameters, inputService, "Enter the parameter format string for the script, e.g. \"--hostname {0}\"");
        }

        void IInstallationPlugin.Default(IOptionsService optionsService)
        {
            _renewal.Script = optionsService.TryGetRequiredOption(nameof(optionsService.Options.Script), optionsService.Options.Script);
            if (!_renewal.Script.ValidFile(_log))
            {
                throw new ArgumentException(nameof(optionsService.Options.Script));
            }
            _renewal.ScriptParameters = optionsService.Options.ScriptParameters;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _renewal.Script,
                  _renewal.ScriptParameters,
                  _renewal.Binding.Host,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name ?? _renewal.CentralSslStore,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  _renewal.CentralSslStore);
        }
    }
}