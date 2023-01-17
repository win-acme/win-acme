using PKISharp.WACS.Plugins.Base.Factories;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullOptionsFactory : PluginOptionsFactory<NullOptions>
    {
        public override int Order => int.MaxValue;
    }

}
