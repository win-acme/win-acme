using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : ScriptClient, IInstallationPlugin
    {
        private Renewal _renewal;
        private ScriptOptions _options;

        public Script(Renewal renewal, ScriptOptions options, ILogService logService) : base(logService)
        {
            _options = options;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _options.Script,
                  _options.ScriptParameters,
                  _renewal.FriendlyName,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name ?? newCertificate.PfxFile.Directory.FullName,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  newCertificate.PfxFile.Directory.FullName);
        }
    }
}