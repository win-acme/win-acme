using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginBackend<TBackend, TCapability>
        where TCapability : IPluginCapability
        where TBackend : IPlugin
    {
        public TBackend Backend { get; }
        public TCapability Capability { get; }
        public Plugin Meta { get; }

        public PluginBackend(Plugin meta, TBackend backend, TCapability capability)
        {
            Meta = meta;
            Backend = backend;
            Capability = capability;
        }
    }
}
