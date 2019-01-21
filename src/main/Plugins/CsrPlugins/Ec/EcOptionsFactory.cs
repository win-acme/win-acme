using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class EcOptionsFactory : CsrPluginOptionsFactory<Ec, EcOptions>
    {
        public EcOptionsFactory(ILogService log) : base(log) { }

        public override EcOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(optionsService);
        }

        public override EcOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<CsrArguments>();
            return new EcOptions() { OcspMustStaple = args.OcspMustStaple };
        }
    }
}
