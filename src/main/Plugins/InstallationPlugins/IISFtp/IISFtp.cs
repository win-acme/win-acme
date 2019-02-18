using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtp : IInstallationPlugin
    {
        private IIISClient _iisClient;
        private ILogService _log;
        private IISFtpOptions _options;

        public IISFtp(IISFtpOptions options, IIISClient iisClient, ILogService log)
        {
            _iisClient = iisClient;
            _options = options;
            _log = log;
        }

        void IInstallationPlugin.Install(IStorePlugin store, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            if (!(store is CertificateStore))
            {
                // Unknown/unsupported store
                var errorMessage = "This installation plugin cannot be used in combination with the store plugin";
                _log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            _iisClient.UpdateFtpSite(_options.SiteId, newCertificate, oldCertificate);
        }
    }
}
