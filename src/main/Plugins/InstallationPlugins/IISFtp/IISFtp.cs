using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtp : IInstallationPlugin
    {
        private IIISClient _iisClient;
        private IISFtpOptions _options;

        public IISFtp(IISFtpOptions options, IIISClient iisClient)
        {
            _iisClient = iisClient;
            _options = options;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            _iisClient.UpdateFtpSite(_options.SiteId, newCertificate, oldCertificate);
        }
    }
}
