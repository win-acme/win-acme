using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class RsaOptionsFactory : CsrPluginOptionsFactory<Rsa, RsaOptions>
    {
        private readonly IArgumentsService _arguments;

        public RsaOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public string Name => "RSA";
        public string Description => "RSA key";

        public override RsaOptions Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override RsaOptions Default()
        {
            var args = _arguments.GetArguments<CsrArguments>();
            return new RsaOptions()
            {
                OcspMustStaple = args.OcspMustStaple ? true : (bool?)null,
                ReusePrivateKey = args.ReusePrivateKey ? true : (bool?)null
            };
        }
    }
}
