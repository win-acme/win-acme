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
            var args = optionsService.GetArguments<CentralSslArguments>();
            var path = args.CentralSslStore;
            while (!path.ValidPath(_log))
            {
                path = inputService.RequestString("Path to Central SSL store");
            }
            var password = args.PfxPassword;
            if (string.IsNullOrEmpty(password))
            {
                password = inputService.ReadPassword("Password to use for the PFX files, or enter for none");
            }
            return new CentralSslOptions
            {
                Path = path,
                KeepExisting = args.KeepExisting,
                PfxPassword = string.IsNullOrWhiteSpace(password) ? null : password
            };
        }

        public override CentralSslOptions Default(IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<CentralSslArguments>();
            var path = optionsService.TryGetRequiredOption(nameof(args.CentralSslStore), args.CentralSslStore);
            if (path.ValidPath(_log))
            {
                return new CentralSslOptions
                {
                    Path = path,
                    KeepExisting = args.KeepExisting,
                    PfxPassword = args.PfxPassword
                };
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }
    }
}
