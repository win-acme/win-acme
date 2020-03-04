using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreOptionsFactory : StorePluginOptionsFactory<CertificateStore, CertificateStoreOptions>
    {
        private readonly IArgumentsService _arguments;

        public CertificateStoreOptionsFactory(UserRoleService userRoleService, IArgumentsService arguments)
        {
            _arguments = arguments;
            Disabled = CertificateStore.Disabled(userRoleService);
        }

        public override Task<CertificateStoreOptions?> Aquire(IInputService inputService, RunLevel runLevel) => Default();

        public override async Task<CertificateStoreOptions?> Default()
        {
            var args = _arguments.GetArguments<CertificateStoreArguments>();
            var ret = new CertificateStoreOptions {
                StoreName = args.CertificateStore,
                KeepExisting = args.KeepExisting,
                AclFullControl = args.AclFullControl.ParseCsv()
            };
            return ret;
        }
    }
}
