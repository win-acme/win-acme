using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class UserStoreCapability : DefaultCapability
    {
        private readonly AdminService _adminService;
        public UserStoreCapability(AdminService adminService) =>
            _adminService = adminService;

        public override State State =>
            _adminService.IsSystem ?
            State.DisabledState("The .") :
            State.EnabledState();
    }
}
