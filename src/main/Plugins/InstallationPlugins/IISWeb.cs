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
        private ScheduledRenewal _renewal;
        private ILogService _log;
        private ITargetPlugin _targetPlugin;
        private IISClient _iisClient;
        private IISWebOptions _options;

        public IISWeb(ScheduledRenewal renewal, IISWebOptions options, IISClient iisClient, ITargetPlugin targetPlugin, ILogService log) 
        {
            _iisClient = iisClient;
            _renewal = renewal;
            _targetPlugin = targetPlugin;
            _log = log;
            _options = options;
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

            foreach (var split in _targetPlugin.Split(_renewal.Target))
            {
                _iisClient.AddOrUpdateBindings(split, bindingOptions, oldThumb);
            }
        }
    }
}
