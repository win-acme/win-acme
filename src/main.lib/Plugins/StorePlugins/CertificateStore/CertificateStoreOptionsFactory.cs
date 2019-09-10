using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        private readonly IArgumentsService _arguments;

        public CertificateStoreOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public override Task<CertificateStoreOptions> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override Task<CertificateStoreOptions> Default()
        {
            var args = _arguments.GetArguments<CertificateStoreArguments>();
            return Task.FromResult(new CertificateStoreOptions
            {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting
            });
        }
    }
}
