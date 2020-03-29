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
    internal class IISFtp : IInstallationPlugin
    {
        private readonly IIISClient _iisClient;
        private readonly ILogService _log;
        private readonly IISFtpOptions _options;
        private readonly IUserRoleService _userRoleService;

        public IISFtp(IISFtpOptions options, IIISClient iisClient, ILogService log, IUserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _options = options;
            _log = log;
            _userRoleService = userRoleService;
        }

        Task IInstallationPlugin.Install(IEnumerable<IStorePlugin> stores, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            if (!stores.Any(x => x is CertificateStore))
            {
                // Unknown/unsupported store
                var errorMessage = "This installation plugin cannot be used in combination with the store plugin";
                _log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            _iisClient.UpdateFtpSite(_options.SiteId, newCertificate, oldCertificate);
            return Task.CompletedTask;
        }

        (bool, string?) IPlugin.Disabled => Disabled(_userRoleService, _iisClient);

        internal static (bool, string?) Disabled(IUserRoleService userRoleService, IIISClient iisClient)
        {
            var (allow, reason) = userRoleService.AllowIIS;
            if (!allow)
            {
                return (true, reason);
            }
            if (!iisClient.HasFtpSites)
            {
                return (true, "No IIS ftp sites available.");
            }
            return (false, null);
        }
    }
}
