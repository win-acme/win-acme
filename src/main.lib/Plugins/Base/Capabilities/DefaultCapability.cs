using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public class DefaultCapability : IPluginCapability
    {
        public virtual (bool, string?) Disabled => (false, null);
    }
}
