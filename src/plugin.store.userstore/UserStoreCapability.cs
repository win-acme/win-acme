using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class UserStoreCapability : DefaultCapability
    {
        private readonly AdminService _adminService;
        public UserStoreCapability(AdminService adminService) =>
            _adminService = adminService;

        public override State State =>
            _adminService.IsSystem ?
            State.DisabledState("It doesn't make sense to use the user store plugin while running as SYSTEM.") :
            State.EnabledState();
    }
}
