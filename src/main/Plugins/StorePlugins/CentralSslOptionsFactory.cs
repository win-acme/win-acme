using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptionsFactory : StorePluginOptionsFactory<CentralSsl, CentralSslOptions>
    {
        public CentralSslOptionsFactory(ILogService log) : base(log) { }

        public override CentralSslOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var path = optionsService.Options.CentralSslStore;
            while (!path.ValidPath(_log))
            {
                path = inputService.RequestString("Path to Central SSL store");
            }
            return new CentralSslOptions
            {
                Path = path,
                KeepExisting = optionsService.Options.KeepExisting,
                PfxPassword = inputService.ReadPassword("Password to use for the PFX files")
            };
        }

        public override CentralSslOptions Default(IOptionsService optionsService)
        {
            var path = optionsService.TryGetRequiredOption(nameof(optionsService.Options.CentralSslStore), optionsService.Options.CentralSslStore);
            if (path.ValidPath(_log))
            {
                return new CentralSslOptions
                {
                    Path = path,
                    KeepExisting = optionsService.Options.KeepExisting,
                    PfxPassword = optionsService.Options.PfxPassword
                };
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }
    }
}
