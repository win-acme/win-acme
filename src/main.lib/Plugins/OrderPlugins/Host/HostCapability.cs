using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class HostCapability : OrderCapability
    {
        protected readonly Target Target;
        public HostCapability(Target target) => Target = target;
        public virtual bool CanProcess() => Target.UserCsrBytes == null;
    }
}
