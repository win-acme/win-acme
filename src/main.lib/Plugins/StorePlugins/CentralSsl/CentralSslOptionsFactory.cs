using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptionsFactory : StorePluginOptionsFactory<CentralSsl, CentralSslOptions>
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;
        private readonly ISettingsService _settings;

        public CentralSslOptionsFactory(ILogService log, ISettingsService settings, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        public override Task<CentralSslOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<CentralSslArguments>();

            // Get path from command line, default setting or user input
            var path = args.CentralSslStore;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _settings.DefaultCentralSslStore;
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = input.RequestString("Path to Central Certificate Store");
            }

            // Get password from command line, default setting or user input
            var password = args.PfxPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                password = _settings.DefaultCentralSslPfxPassword;
            }
            if (string.IsNullOrEmpty(password))
            {
                password = input.ReadPassword("Password to use for the PFX files, or enter for none");
            }
            return Create(path, password, args.KeepExisting);
        }

        public override Task<CentralSslOptions> Default()
        {
            var args = _arguments.GetArguments<CentralSslArguments>();
            var path = _settings.DefaultCentralSslStore;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _arguments.TryGetRequiredArgument(nameof(args.CentralSslStore), args.CentralSslStore);
            }

            var password = _settings.DefaultCentralSslPfxPassword;
            if (!string.IsNullOrWhiteSpace(args.PfxPassword))
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

        private Task<CentralSslOptions> Create(string path, string password, bool keepExisting)
        {
            var ret = new CentralSslOptions
            {
                KeepExisting = keepExisting
            };
            if (!string.IsNullOrWhiteSpace(password) && !string.Equals(password, _settings.DefaultCentralSslPfxPassword))
            {
                ret.PfxPassword = new ProtectedString(password);
            }
            if (!string.Equals(path, _settings.DefaultCentralSslStore, StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return Task.FromResult(ret);
        }
    }
}
