using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWeb : IInstallationPlugin
    {
        private readonly ILogService _log;
        private readonly IIISClient _iisClient;
        private readonly IISWebOptions _options;
        private readonly IUserRoleService _userRoleService;

        public IISWeb(IISWebOptions options, IIISClient iisClient, ILogService log, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _log = log;
            _options = options;
            _userRoleService = userRoleService;
        }

        Task<bool> IInstallationPlugin.Install(Target target, IEnumerable<IStorePlugin> stores, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            var bindingOptions = new BindingOptions().
                WithThumbprint(newCertificate.Certificate.GetCertHash());

            var centralSsl = stores.FirstOrDefault(x => x is CentralSsl);
            var certificateStore = stores.FirstOrDefault(x => x is CertificateStore);

            if (centralSsl != null)
            {
                if (_iisClient.Version.Major < 8)
                {
                    var errorMessage = "Centralized SSL is only supported on IIS8+";
                    _log.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
                else
                {
                    bindingOptions = bindingOptions.WithFlags(SSLFlags.CentralSsl);
                }
            }
            else if (certificateStore != null)
            {
                bindingOptions = bindingOptions.WithStore(newCertificate.StoreInfo[typeof(CertificateStore)].Path);
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
            foreach (var part in target.Parts)
            {
                _iisClient.AddOrUpdateBindings(
                    part.Identifiers.OfType<DnsIdentifier>(),
                    bindingOptions.WithSiteId(_options.SiteId ?? part.SiteId),
                    oldThumb);
            }

            return Task.FromResult(true);
        }

        (bool, string?) IPlugin.Disabled => Disabled(_userRoleService, _iisClient);

        internal static (bool, string?) Disabled(IUserRoleService userRoleService, IIISClient iisClient)
        {
            var (allow, reason) = userRoleService.AllowIIS;
            if (!allow)
            {
                return (true, reason);
            }
            if (!iisClient.HasWebSites)
            {
                return (true, "No IIS websites available.");
            }
            return (false, null);
        }
    }
}
