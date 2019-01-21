using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class RsaOptionsFactory : CsrPluginOptionsFactory<Rsa, RsaOptions>
    {
        public RsaOptionsFactory(ILogService log) : base(log) { }
        public string Name => "RSA";
        public string Description => "RSA key";

        public override RsaOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(optionsService);
        }

        public override RsaOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<CsrArguments>();
            return new RsaOptions() { OcspMustStaple = args.OcspMustStaple };
        }
    }
}
