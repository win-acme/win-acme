using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class RsaOptions : CsrPluginOptions<Rsa>
    {
        public override string Name => "RSA";
        public override string Description => "Standard RSA key pair";
    }
}
