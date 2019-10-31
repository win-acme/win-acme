using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    [Plugin("9aadcf71-5241-4c4f-aee1-bfe3f6be3489")]
    internal class EcOptions : CsrPluginOptions<Ec>
    {
        public override string Name => "EC";
        public override string Description => "Elliptic Curve key";
    }
}
