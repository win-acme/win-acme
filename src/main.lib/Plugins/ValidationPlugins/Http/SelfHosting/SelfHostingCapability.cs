using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class SelfHostingCapability : HttpValidationCapability
    {
        protected readonly IUserRoleService UserRoleService;
        public SelfHostingCapability(Target target, IUserRoleService user) : base(target) => UserRoleService = user;
        public override (bool, string?) Disabled
        {
            get
            {
                if (!UserRoleService.AllowSelfHosting)
                {
                    return (true, "Run as administrator to allow opening a TCP listener.");
                }
                else
                {
                    return (false, null);
                }
            }
        }
    }
}
