using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWeb : IInstallationPlugin
    {
        private Target _target;
        private ILogService _log;
        private IISClient _iisClient;
        private IISWebOptions _options;

        public IISWeb(Target target, IISWebOptions options, IISClient iisClient, ILogService log) 
        {
            _iisClient = iisClient;
            _log = log;
            _options = options;
            _target = target;
        }

        void IInstallationPlugin.Install(CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            SSLFlags flags = 0;
            if (newCertificate.Store == null)
            {
                if (_iisClient.Version.Major < 8)
                {
                    var errorMessage = "Centralized SSL is only supported on IIS8+";
                    _log.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    flags |= SSLFlags.CentralSSL;
                }
            }
            var bindingOptions = new BindingOptions().
                WithFlags(flags).
                WithStore(newCertificate.Store?.Name).
                WithSiteId(_options.SiteId).
                WithThumbprint(newCertificate.Certificate.GetCertHash());

            var oldThumb = oldCertificate?.Certificate?.GetCertHash();

            foreach (var part in _target.Parts)
            {
                _iisClient.AddOrUpdateBindings(part.Hosts, bindingOptions.WithSiteId(part.SiteId), oldThumb);
            }
        }
    }
}
