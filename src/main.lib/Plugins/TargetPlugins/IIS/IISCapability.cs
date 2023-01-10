using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISCapability : DefaultCapability
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
    }
}
