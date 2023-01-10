using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Tls
{
    internal class SelfHostingCapability : TlsValidationCapability
    {
        protected readonly IUserRoleService UserRoleService;
        public SelfHostingCapability(Target target, IUserRoleService user) : base(target) => UserRoleService = user;
        public override State State =>
            base.State.Disabled ? 
                base.State :
                UserRoleService.AllowSelfHosting ? 
                    State.EnabledState() :
                    State.DisabledState("Run as administrator to allow opening a TCP listener.");
    }
}
