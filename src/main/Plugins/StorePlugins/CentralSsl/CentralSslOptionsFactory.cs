using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptionsFactory : StorePluginOptionsFactory<CentralSsl, CentralSslOptions>
    {
        public CentralSslOptionsFactory(ILogService log) : base(log) { }

        public override CentralSslOptions Aquire(IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<CentralSslArguments>();
            var path = args.CentralSslStore;
            while (!path.ValidPath(_log))
            {
                path = input.RequestString("Path to Central SSL store");
            }
            var password = args.PfxPassword;
            if (string.IsNullOrEmpty(password))
            {
                password = input.ReadPassword("Password to use for the PFX files, or enter for none");
            }
            return new CentralSslOptions
            {
                Path = path,
                KeepExisting = args.KeepExisting,
                PfxPassword = string.IsNullOrWhiteSpace(password) ? null : password
            };
        }

        public override CentralSslOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CentralSslArguments>();
            var path = arguments.TryGetRequiredArgument(nameof(args.CentralSslStore), args.CentralSslStore);
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
