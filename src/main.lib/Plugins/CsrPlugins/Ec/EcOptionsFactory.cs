using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class EcOptionsFactory : CsrPluginOptionsFactory<Ec, EcOptions>
    {
        private readonly IArgumentsService _arguments;

        public EcOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public override Task<EcOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override Task<EcOptions> Default()
        {
            var args = _arguments.GetArguments<CsrArguments>();
            return Task.FromResult(new EcOptions()
            {
                OcspMustStaple = args.OcspMustStaple ? true : (bool?)null,
                ReusePrivateKey = args.ReusePrivateKey ? true : (bool?)null
            });
        }
    }
}
