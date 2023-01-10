using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public class DefaultCapability : IPluginCapability
    {
        public virtual State State => State.EnabledState();
    }
}
