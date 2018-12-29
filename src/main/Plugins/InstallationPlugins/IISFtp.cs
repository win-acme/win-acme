using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtp : IInstallationPlugin
    {
        private Renewal _renewal;
        private IISClient _iisClient;
        private IISFtpOptions _options;

        public IISFtp(Renewal renewal, IISFtpOptions options, IISClient iisClient)
        {
            _iisClient = iisClient;
            _renewal = renewal;
            _options = options;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            _iisClient.UpdateFtpSite(_options.SiteId, SSLFlags.None, newCertificate, oldCertificate);
        }
    }
}
