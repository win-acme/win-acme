using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [Plugin("b705fa7c-1152-4436-8913-e433d7f84c82")]
    internal class SingleOptions : OrderPluginOptions<Single>
    {
        public override string Name => "Single";
        public override string Description => "Single certificate";
    }
}
