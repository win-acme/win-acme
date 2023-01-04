using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    public class DomainCapability : OrderCapability
    {
        protected readonly Target Target;
        public DomainCapability(Target target) => Target = target;
        public virtual bool CanProcess() => Target.UserCsrBytes == null;
    }
}
