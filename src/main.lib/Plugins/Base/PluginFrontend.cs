using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginFrontend<TCapability, TOptions>
        where TCapability : IPluginCapability
        where TOptions : PluginOptions, new()
    {
        public IPluginOptionsFactory<TOptions> OptionsFactory { get; }
        public TCapability Capability { get; }
        public Plugin Meta { get; }

        public PluginFrontend(Plugin meta, IPluginOptionsFactory<TOptions> factory, TCapability capability)
        {
            Meta = meta;
            OptionsFactory = factory;
            Capability = capability;
        }
    }
}
