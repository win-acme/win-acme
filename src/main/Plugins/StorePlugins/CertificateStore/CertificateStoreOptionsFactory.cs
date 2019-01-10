using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStorePluginOptions>
    {
        public CertificateStoreOptionsFactory(ILogService log) : base(log) { }

        public override CertificateStorePluginOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(optionsService);
        }

        public override CertificateStorePluginOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<CertificateStoreArguments>();
            return new CertificateStorePluginOptions {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting
            };
        }
    }
}
