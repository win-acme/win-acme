using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [Plugin("b7c331d4-d875-453e-b83a-2b537ca12535")]
    internal class DomainOptions : OrderPluginOptions<Domain>
    {
        public override string Name => "Domain";
        public override string Description => "Separate certificate for each domain (e.g. *.example.com)";
    }
}
