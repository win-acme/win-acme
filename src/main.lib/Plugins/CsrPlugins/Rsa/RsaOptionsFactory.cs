using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class RsaOptionsFactory : CsrPluginOptionsFactory<Rsa, RsaOptions>
    {
        private readonly IArgumentsService _arguments;

        public RsaOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public string Name => "RSA";
        public string Description => "RSA key";

        public override Task<RsaOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override Task<RsaOptions> Default()
        {
            var args = _arguments.GetArguments<CsrArguments>();
            return Task.FromResult(new RsaOptions()
            {
                OcspMustStaple = args.OcspMustStaple ? true : (bool?)null,
                ReusePrivateKey = args.ReusePrivateKey ? true : (bool?)null
            });
        }
    }
}
