using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        private IArgumentsService _arguments;

        public CertificateStoreOptionsFactory(IArgumentsService arguments)
        {
            _arguments = arguments;
        }

        public override CertificateStoreOptions Aquire(IInputService inputService, RunLevel runLevel)
        {
            return Default();
        }

        public override CertificateStoreOptions Default()
        {
            var args = _arguments.GetArguments<CertificateStoreArguments>();
            return new CertificateStoreOptions
            {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting
            };
        }
    }
}
