using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        public CertificateStoreOptionsFactory(ILogService log) : base(log) { }

        public override CertificateStoreOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Default(arguments);
        }

        public override CertificateStoreOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CertificateStoreArguments>();
            return new CertificateStoreOptions
            {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting
            };
        }
    }
}
