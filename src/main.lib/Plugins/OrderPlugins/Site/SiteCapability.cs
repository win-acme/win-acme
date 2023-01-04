using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using System.Linq;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class SiteCapability : OrderCapability
    {
        protected readonly Target Target;
        public SiteCapability(Target target) => Target = target;
        public virtual bool CanProcess()
        {
            if (Target.UserCsrBytes != null)
            {
                return false;
            }
            return Target.Parts.Any(p => p.SiteId > 0);
        }
    }
}
