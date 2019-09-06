using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

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

        void IInstallationPlugin.Install(IEnumerable<IStorePlugin> stores, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            if (!stores.Any(x => x is CertificateStore))
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
