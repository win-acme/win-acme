using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreCapability : DefaultCapability
    {
        private readonly IUserRoleService _userRoleService;
        public CertificateStoreCapability(IUserRoleService userRoleService) =>
            _userRoleService = userRoleService;

        public override State State =>
            _userRoleService.AllowCertificateStore ? 
            State.EnabledState() : 
            State.DisabledState("Run as administrator to allow certificate store access.");
    }
}
