using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginBackend<TBackend, TCapability, TOptions>
        where TCapability : IPluginCapability
        where TBackend : IPlugin
        where TOptions : PluginOptions
    {
        public TBackend Backend { get; }
        public TCapability Capability { get; }
        public Plugin Meta { get; }
        public TOptions Options { get; }

        public PluginBackend(Plugin meta, TBackend backend, TCapability capability, TOptions options)
        {
            Meta = meta;
            Backend = backend;
            Capability = capability;
            Options = options;
        }
    }
}
