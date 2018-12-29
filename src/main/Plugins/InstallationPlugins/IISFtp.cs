using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtp : IInstallationPlugin
    {
        private IISClient _iisClient;
        private IISFtpOptions _options;

        public IISFtp(IISFtpOptions options, IISClient iisClient)
        {
            _iisClient = iisClient;
            _options = options;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            _iisClient.UpdateFtpSite(_options.SiteId, SSLFlags.None, newCertificate, oldCertificate);
        }
    }
}
