using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    [Plugin("b9060d4b-c2d3-49ac-b37f-962e7c3cbe9d")]
    internal class RsaOptions : CsrPluginOptions<Rsa>
    {
        public override string Name => "RSA";
        public override string Description => "RSA key";
    }
}
