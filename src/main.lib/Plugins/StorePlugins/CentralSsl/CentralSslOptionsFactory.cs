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
        private readonly ArgumentsInputService _argumentInput;
        private readonly ISettingsService _settings;
        private readonly SecretServiceManager _secretServiceManager;

        public CentralSslOptionsFactory(
            ILogService log, 
            ISettingsService settings,
            IArgumentsService arguments,
            ArgumentsInputService argumentInput,
            SecretServiceManager secretServiceManager)
        {
            _log = log;
            _arguments = arguments;
            _argumentInput = argumentInput;
            _settings = settings;
            _secretServiceManager = secretServiceManager;
        }

        public override async Task<CentralSslOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var path = await _argumentInput.
                GetString<CentralSslArguments>(args => args.CentralSslStore).
                WithDefault(CentralSsl.DefaultPath(_settings)).
                Interactive("Store path", input, runLevel).
                Required().
                Validate(x => x.ValidPath(_log), "Invalid path").
                GetValue();

            // Get password from command line, default setting or user input
            var args = _arguments.GetArguments<CentralSslArguments>();
            var password = args?.PfxPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                password = CentralSsl.DefaultPassword(_settings);
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                password = await _secretServiceManager.GetSecret("Password to use for the .pfx files", password);
            }
            return Create(path, password);
        }

        public override async Task<CentralSslOptions?> Default()
        {
            var path = await _argumentInput.
                GetString<CentralSslArguments>(args => args.CentralSslStore).
                WithDefault(CentralSsl.DefaultPath(_settings)).
                Required().
                Validate(x => x.ValidPath(_log), "Invalid path").
                GetValue();

            var password = await _argumentInput.
                GetProtectedString<CentralSslArguments>(args => args.PfxPassword).
                WithDefault(CentralSsl.DefaultPassword(_settings).Protect()).
                GetValue();
           
            return Create(path, password?.Value);
        }

        private CentralSslOptions Create(string? path, string? password)
        {
            var ret = new CentralSslOptions
            {
                KeepExisting = false
            };
            if (!string.IsNullOrWhiteSpace(password) && !string.Equals(password, CentralSsl.DefaultPassword(_settings)))
            {
                ret.PfxPassword = new ProtectedString(password);
            }
            if (!string.Equals(path, CentralSsl.DefaultPath(_settings), StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }
}
