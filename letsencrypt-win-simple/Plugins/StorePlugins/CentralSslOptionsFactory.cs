using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptionsFactory : BaseStorePluginFactory<CentralSsl, CentralSslOptions>
    {
        public CentralSslOptionsFactory(ILogService log) : base(log) { }

        public override CentralSslOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var path = optionsService.Options.CertificateStore;
            while (!path.ValidPath(_log))
            {
                path = inputService.RequestString("Path to Central SSL store");
            }
            return new CentralSslOptions
            {
                Path = path,
                KeepExisting = optionsService.Options.KeepExisting
            };
        }

        public override CentralSslOptions Default(IOptionsService optionsService)
        {
            var path = optionsService.TryGetRequiredOption(nameof(optionsService.Options.CertificateStore), optionsService.Options.CertificateStore);
            if (path.ValidPath(_log))
            {
                return new CentralSslOptions
                {
                    Path = path,
                    KeepExisting = optionsService.Options.KeepExisting
                };
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }
    }
}
