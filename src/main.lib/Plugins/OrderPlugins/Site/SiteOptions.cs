using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [Plugin("74a42b2d-8eaa-4f40-ab6a-f55304254143")]
    internal class SiteOptions : OrderPluginOptions<Site>
    {
        public override string Name => "Site";
        public override string Description => "Separate certificate for each IIS site";
    }
}
