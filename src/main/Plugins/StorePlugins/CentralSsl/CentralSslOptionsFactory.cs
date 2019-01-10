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
            var path = optionsService.MainArguments.CentralSslStore;
            while (!path.ValidPath(_log))
            {
                path = inputService.RequestString("Path to Central SSL store");
            }
            return new CentralSslOptions
            {
                Path = path,
                KeepExisting = optionsService.MainArguments.KeepExisting,
                PfxPassword = inputService.ReadPassword("Password to use for the PFX files")
            };
        }

        public override CentralSslOptions Default(IOptionsService optionsService)
        {
            var path = optionsService.TryGetRequiredOption(nameof(optionsService.MainArguments.CentralSslStore), optionsService.MainArguments.CentralSslStore);
            if (path.ValidPath(_log))
            {
                return new CentralSslOptions
                {
                    Path = path,
                    KeepExisting = optionsService.MainArguments.KeepExisting,
                    PfxPassword = optionsService.MainArguments.PfxPassword
                };
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }
    }
}
