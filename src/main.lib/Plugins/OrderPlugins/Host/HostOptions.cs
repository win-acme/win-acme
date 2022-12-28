using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    internal class HostOptions : OrderPluginOptions<Host>
    {
        public override string Name => "Host";
        public override string Description => "Separate certificate for each host (e.g. sub.example.com)";
    }
}
