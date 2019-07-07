using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class RsaOptionsFactory : CsrPluginOptionsFactory<Rsa, RsaOptions>
    {
        public RsaOptionsFactory(ILogService log) : base(log) { }
        public string Name => "RSA";
        public string Description => "RSA key";

        public override RsaOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Default(arguments);
        }

        public override RsaOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CsrArguments>();
            return new RsaOptions()
            {
                OcspMustStaple = args.OcspMustStaple,
                ReusePrivateKey = args.ReusePrivateKey
            };
        }
    }
}
