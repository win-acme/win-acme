using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Capabilities;
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
    }
}
