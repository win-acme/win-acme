using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class EcOptionsFactory : CsrPluginOptionsFactory<Ec, EcOptions>
    {
        public EcOptionsFactory(ArgumentsInputService arguments) : base(arguments) { }

        public override Task<EcOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override async Task<EcOptions> Default()
        {
            return new EcOptions()
            {
                OcspMustStaple = await OcspMustStaple.GetValue(),
                ReusePrivateKey = await ReusePrivateKey.GetValue()
            };
        }
    }
}
