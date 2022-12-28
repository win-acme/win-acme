using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class SingleOptions : OrderPluginOptions<Single>
    {
        public override string Name => "Single";
        public override string Description => "Single certificate";
    }
}
