using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWeb : IInstallationPlugin
    {
        private Target _target;
        private ILogService _log;
        private IIISClient _iisClient;
        private IISWebOptions _options;

        public IISWeb(Target target, IISWebOptions options, IIISClient iisClient, ILogService log) 
        {
            _iisClient = iisClient;
            _log = log;
            _options = options;
            _target = target;
        }

        void IInstallationPlugin.Install(IStorePlugin store, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            var bindingOptions = new BindingOptions().
                WithThumbprint(newCertificate.Certificate.GetCertHash());

            if (store is CentralSsl)
            {
                if (_iisClient.Version.Major < 8)
                {
                    var errorMessage = "Centralized SSL is only supported on IIS8+";
                    _log.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    bindingOptions = bindingOptions.WithFlags(SSLFlags.CentralSSL);
                }
            }
            else if (store is CertificateStore)
            {
                bindingOptions = bindingOptions.WithStore(newCertificate.StorePath);
            }
            else
            {
                // Unknown/unsupported store
                var errorMessage = "This installation plugin cannot be used in combination with the store plugin";
                _log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Optionaly overrule the standard IP for new bindings 
            if (!string.IsNullOrEmpty(_options.NewBindingIp))
            {
                bindingOptions = bindingOptions.WithIP(_options.NewBindingIp);
            }

            // Optionaly overrule the standard port for new bindings 
            if (_options.NewBindingPort > 0)
            {
                bindingOptions = bindingOptions.WithPort(_options.NewBindingPort.Value);
            }

            var oldThumb = oldCertificate?.Certificate?.GetCertHash();
            foreach (var part in _target.Parts)
            {
                _iisClient.AddOrUpdateBindings(
                    part.Identifiers, 
                    bindingOptions.WithSiteId(_options.SiteId ?? part.SiteId), 
                    oldThumb);
            }
        }
    }
}
