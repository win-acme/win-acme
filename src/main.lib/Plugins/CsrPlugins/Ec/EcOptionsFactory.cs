using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class EcOptionsFactory : CsrPluginOptionsFactory<Ec, EcOptions>
    {
        private IArgumentsService _arguments;

        public EcOptionsFactory(IArgumentsService arguments)
        {
            _arguments = arguments;
        }

        public override EcOptions Aquire(IInputService inputService, RunLevel runLevel)
        {
            return Default();
        }

        public override EcOptions Default()
        {
            var args = _arguments.GetArguments<CsrArguments>();
            return new EcOptions() {
                OcspMustStaple = args.OcspMustStaple ? true : (bool?)null,
                ReusePrivateKey = args.ReusePrivateKey ? true : (bool?)null
            };
        }
    }
}
