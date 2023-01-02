using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [IPlugin.Plugin<IISFtpOptions, IISOptionsFactory, WacsJsonPlugins>
        ("13058a79-5084-48af-b047-634e0ee222f4", "IISFTP", "Create or update FTP bindings in IIS", Hidden = true)]
    [IPlugin.Plugin<IISOptions, IISOptionsFactory, WacsJsonPlugins>
        ("ea6a5be3-f8de-4d27-a6bd-750b619b2ee2", "IIS", "Create or update bindings in IIS")]
    internal class IIS : IInstallationPlugin
    {
        private readonly ILogService _log;
        private readonly IIISClient _iisClient;
        private readonly IISOptions _options;
        private readonly IUserRoleService _userRoleService;

        public IIS(IISFtpOptions options, IIISClient iisClient, ILogService log, IUserRoleService userRoleService) : this((IISOptions)options, iisClient, log, userRoleService) { }
        public IIS(IISOptions options, IIISClient iisClient, ILogService log, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _log = log;
            _options = options;
            _userRoleService = userRoleService;
        }

        Task<bool> IInstallationPlugin.Install(
            Target source, 
            IEnumerable<Type> stores,
            CertificateInfo newCertificate,
            CertificateInfo? oldCertificate)
        {
            // Store validation
            var centralSslForHttp = false;
            var centralSsl = stores.Contains(typeof(CentralSsl));
            var certificateStore = stores.Contains(typeof(CertificateStore));
            if (centralSsl == null && certificateStore == null)
            {
                // No supported store
                var errorMessage = "The IIS installation plugin requires the CertificateStore and/or CentralSsl store plugin";
                _log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
 
            // Determine site types
            if (_options.SiteId != null)
            {
                var siteType = _iisClient.GetSite(_options.SiteId.Value).Type; 
                foreach (var part in source.Parts)
                {
                    part.SiteId = _options.SiteId;
                    part.SiteType = siteType;
                }
            }

            if (centralSsl != null)
            {
                centralSslForHttp = true;
                var supported = true;
                var reason = "";
                if (_iisClient.Version.Major < 8)
                {
                    reason = "CentralSsl store requires IIS version 8.0 or higher";
                    supported = false;
                    centralSslForHttp = false;
                }
                if (source.Parts.Any(p => p.SiteType == IISSiteType.Ftp)) 
                {
                    reason = "CentralSsl store is not supported for FTP sites";
                    supported = false;
                }
                if (!supported && certificateStore == null)
                {
                    // Only throw error if there is no fallback 
                    // available to the CertificateStore plugin.
                    _log.Error(reason);
                    throw new InvalidOperationException(reason);
                } 
            }

            foreach (var part in source.Parts)
            {
                var httpIdentifiers = part.Identifiers.OfType<DnsIdentifier>();
                var bindingOptions = new BindingOptions();

                // Pick between CentralSsl and CertificateStore
                bindingOptions = centralSslForHttp
                    ? bindingOptions.
                        WithFlags(SSLFlags.CentralSsl)
                    : bindingOptions.
                        WithThumbprint(newCertificate.Certificate.GetCertHash()).
                        WithStore(newCertificate.StoreInfo[typeof(CertificateStore)].Path);

                switch (part.SiteType)
                {
                    case IISSiteType.Web:
                        // Optionaly overrule the standard IP for new bindings 
                        if (!string.IsNullOrEmpty(_options.NewBindingIp))
                        {
                            bindingOptions = bindingOptions.
                                WithIP(_options.NewBindingIp);
                        }
                        // Optionaly overrule the standard port for new bindings 
                        if (_options.NewBindingPort > 0)
                        {
                            bindingOptions = bindingOptions.
                                WithPort(_options.NewBindingPort.Value);
                        }
                        bindingOptions = bindingOptions.WithSiteId(part.SiteId!.Value);
                        _iisClient.UpdateHttpSite(httpIdentifiers, bindingOptions, oldCertificate?.Certificate.GetCertHash(), newCertificate.SanNames);
                        if (certificateStore != null) 
                        {
                            _iisClient.UpdateFtpSite(0, newCertificate, oldCertificate);
                        }
                        break;
                    case IISSiteType.Ftp:
                        // Update FTP site
                        _iisClient.UpdateFtpSite(part.SiteId!.Value, newCertificate, oldCertificate);
                        _iisClient.UpdateHttpSite(httpIdentifiers, bindingOptions, oldCertificate?.Certificate.GetCertHash(), newCertificate.SanNames);
                        break;
                    default:
                        _log.Error("Unknown site type");
                        break;
                }
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
            if (!iisClient.Sites.Any())
            {
                return (true, "No IIS sites available.");
            }
            return (false, null);
        }
    }
}
