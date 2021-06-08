using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class RsaOptionsFactory : CsrPluginOptionsFactory<Rsa, RsaOptions>
    {
        public RsaOptionsFactory(ArgumentsInputService arguments) : base(arguments) { }
        public override Task<RsaOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override async Task<RsaOptions> Default()
        {
            return new RsaOptions()
            {
                OcspMustStaple = await OcspMustStaple.GetValue(),
                ReusePrivateKey = await ReusePrivateKey.GetValue()
            };
        }
    }
}
