using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class EcOptions : CsrPluginOptions<Ec>
    {
        public override string Name => "EC";
        public override string Description => "Elliptic Curve key";
    }
}
