using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class RsaOptions : CsrPluginOptions<Rsa>
    {
        public override string Name => "RSA";
        public override string Description => "RSA key";
    }
}
