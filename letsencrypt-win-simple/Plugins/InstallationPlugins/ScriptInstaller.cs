using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptInstaller : ScriptClient, IInstallationPlugin
    {
        private ScheduledRenewal _renewal;

        public ScriptInstaller(ScheduledRenewal renewal, ILogService logService) : base(logService)
        {
            _renewal = renewal;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            RunScript(
                  _renewal.Script,
                  _renewal.ScriptParameters,
                  _renewal.Target.Host,
                  Properties.Settings.Default.PFXPassword,
                  newCertificate.PfxFile.FullName,
                  newCertificate.Store?.Name ?? newCertificate.PfxFile.Directory.FullName,
                  newCertificate.Certificate.FriendlyName,
                  newCertificate.Certificate.Thumbprint,
                  newCertificate.PfxFile.Directory.FullName);
        }
    }
}