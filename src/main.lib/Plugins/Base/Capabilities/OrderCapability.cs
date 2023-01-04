using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public class OrderCapability : DefaultCapability, IOrderPluginCapability
    {
        public virtual bool CanProcess() => true;
    }
}
