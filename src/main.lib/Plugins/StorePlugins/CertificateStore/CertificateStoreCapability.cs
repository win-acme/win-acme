using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreCapability : DefaultCapability
    {
        private readonly IUserRoleService _userRoleService;
        public CertificateStoreCapability(IUserRoleService userRoleService) =>
            _userRoleService = userRoleService;

        public override (bool, string?) Disabled
        {
            get
            {
                if (_userRoleService.AllowCertificateStore)
                {
                    return (false, null);
                }
                else
                {
                    return (true, "Run as administrator to allow certificate store access.");
                }
            }
        }
    }
}
