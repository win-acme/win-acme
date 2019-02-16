using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Properties;
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

            // Get path from command line, default setting or user input
            var path = args.CentralSslStore;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Settings.Default.DefaultCentralSslStore;
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = input.RequestString("Path to Central Certificate Store");
            }

            // Get password from command line, default setting or user input
            var password = args.PfxPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                password = Settings.Default.DefaultCentralSslPfxPassword;
            }
            if (string.IsNullOrEmpty(password))
            {
                password = input.ReadPassword("Password to use for the PFX files, or enter for none");
            }
            return Create(path, password, args.KeepExisting);
        }

        public override CentralSslOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<CentralSslArguments>();
            var path = Settings.Default.DefaultCentralSslStore;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = arguments.TryGetRequiredArgument(nameof(args.CentralSslStore), args.CentralSslStore);
            }

            var password = Settings.Default.DefaultCentralSslPfxPassword;
            if (string.IsNullOrWhiteSpace(args.PfxPassword))
            {
                password = args.PfxPassword;
            }

            if (path.ValidPath(_log))
            {
                return Create(path, password, args.KeepExisting);
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }

        private CentralSslOptions Create(string path, string password, bool keepExisting)
        {
            var ret = new CentralSslOptions
            {
                KeepExisting = keepExisting
            };
            if (!string.IsNullOrWhiteSpace(password) && !string.Equals(password, Settings.Default.DefaultCentralSslPfxPassword))
            {
                ret.PfxPassword = password;
            }
            if (!string.Equals(path, Settings.Default.DefaultCentralSslStore, StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }
}
