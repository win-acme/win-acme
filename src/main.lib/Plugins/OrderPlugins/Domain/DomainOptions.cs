using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class DomainOptions : OrderPluginOptions<Domain>
    {
        public override string Name => "Domain";
        public override string Description => "Separate certificate for each domain (e.g. *.example.com)";
    }
}
