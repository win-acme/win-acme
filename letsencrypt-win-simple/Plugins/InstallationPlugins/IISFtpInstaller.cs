using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpInstaller : IInstallationPlugin
    {
        private ScheduledRenewal _renewal;
        private IISClient _iisClient;

        public IISFtpInstaller(ScheduledRenewal renewal, IISClient iisClient)
        {
            _iisClient = iisClient;
            _renewal = renewal;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            _iisClient.UpdateFtpSite(_renewal.Target, SSLFlags.None, newCertificate, oldCertificate);
        }
    }
}
