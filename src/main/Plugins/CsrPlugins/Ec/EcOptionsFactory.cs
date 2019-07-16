using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class EcOptionsFactory : CsrPluginOptionsFactory<Ec, EcOptions>
    {
        public EcOptionsFactory(ILogService log) : base(log) { }

        public override EcOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Default(arguments);
        }

        public override EcOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CsrArguments>();
            return new EcOptions() {
                OcspMustStaple = args.OcspMustStaple ? true : (bool?)null,
                ReusePrivateKey = args.ReusePrivateKey ? true : (bool?)null
            };
        }
    }
}
