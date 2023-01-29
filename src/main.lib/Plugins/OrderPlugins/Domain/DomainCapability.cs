using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    public class DomainCapability : DefaultCapability
    {
        protected readonly Target Target;
        public DomainCapability(Target target) => Target = target;
        public override State State => 
            Target.UserCsrBytes == null ? 
            State.EnabledState() : 
            State.DisabledState("Renewals sourced from a custom CSR cannot be split up");
    }
}
