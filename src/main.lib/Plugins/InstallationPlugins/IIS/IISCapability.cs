using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
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

        public override State State
        {
            get
            {
                var state = _userRole.IISState;
                if (state.Disabled)
                {
                    return state;
                }
                if (!_iisClient.Sites.Any())
                {
                    return State.DisabledState("No IIS sites detected.");
                }
                return State.EnabledState();
            }
        }

        public override State CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes)
        {
            if (installationTypes.Contains(typeof(IIS)))
            {
                return State.DisabledState("Cannot be used more than once in a renewal.");
            }
            if (storeTypes.Contains(typeof(CertificateStore)) || storeTypes.Contains(typeof(CentralSsl)))
            {
                return State.EnabledState();
            }
            return State.DisabledState("Requires CertificateStore or CentralSsl store plugin.");
        }
    }
}
