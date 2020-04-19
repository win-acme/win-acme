using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [Plugin("874a86e4-29c7-4294-9ab6-6908866847a0")]
    internal class HostOptions : OrderPluginOptions<Host>
    {
        public override string Name => "Host";
        public override string Description => "Seperate certificate for each host";
    }
}
