using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreFactory : BaseStorePluginFactory<CertificateStore, CertificateStorePluginOptions>
    {
        public CertificateStoreFactory(ILogService log) : base(log) { }

        public override CertificateStorePluginOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Default(optionsService);
        }

        public override CertificateStorePluginOptions Default(IOptionsService optionsService)
        {
            return new CertificateStorePluginOptions {
                StoreName = optionsService.Options.CertificateStore,
                KeepExisting = optionsService.Options.KeepExisting
            };
        }
    }
}
