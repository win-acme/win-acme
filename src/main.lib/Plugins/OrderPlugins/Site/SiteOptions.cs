using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class SiteOptions : OrderPluginOptions<Site>
    {
        public override string Name => "Site";
        public override string Description => "Separate certificate for each IIS site";
    }
}
