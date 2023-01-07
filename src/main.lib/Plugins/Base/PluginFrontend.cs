using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Plugins.Base
{
    internal class PluginFrontend<TFactory, TCapability>
        where TCapability : IPluginCapability
        where TFactory : IPluginOptionsFactory
    {
        public TFactory OptionsFactory { get; }
        public TCapability Capability { get; }
        public Plugin Meta { get; }

        public PluginFrontend(Plugin meta, TFactory factory, TCapability capability)
        {
            Meta = meta;
            OptionsFactory = factory;
            Capability = capability;
        }
    }
}
