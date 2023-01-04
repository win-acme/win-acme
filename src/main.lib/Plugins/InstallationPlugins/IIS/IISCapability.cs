using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISCapability : InstallationCapability
    {
        private readonly IUserRoleService _userRole;
        private readonly IIISClient _iisClient;

        public IISCapability(IUserRoleService userRole, IIISClient iisClient) 
        {
            _userRole = userRole;
            _iisClient = iisClient;
        }

        public override (bool, string?) Disabled
        {
            get
            {
                var (allow, reason) = _userRole.AllowIIS;
                if (!allow)
                {
                    return (true, reason);
                }
                if (!_iisClient.Sites.Any())
                {
                    return (true, "No IIS sites available.");
                }
                return (false, null);
            }
        }

        public override (bool, string?) CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes)
        {
            if (installationTypes.Contains(typeof(IIS)))
            {
                return (false, "Cannot be used more than once in a renewal.");
            }
            if (storeTypes.Contains(typeof(CertificateStore)) || storeTypes.Contains(typeof(CentralSsl)))
            {
                return (true, null);
            }
            return (false, "Requires CertificateStore or CentralSsl store plugin.");
        }
    }
}
