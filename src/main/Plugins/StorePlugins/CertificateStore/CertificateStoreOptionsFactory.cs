using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        public CertificateStoreOptionsFactory(ILogService log) : base(log) { }

        public override CertificateStoreOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(optionsService);
        }

        public override CertificateStoreOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<CertificateStoreArguments>();
            return new CertificateStoreOptions
            {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting
            };
        }
    }
}
