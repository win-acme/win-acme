using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptionsFactory : StorePluginOptionsFactory<CentralSsl, CentralSslOptions>
    {
        private readonly ILogService _log;
        private readonly ArgumentsInputService _argumentInput;
        private readonly ISettingsService _settings;
        private readonly SecretServiceManager _secretServiceManager;

        public CentralSslOptionsFactory(
            ILogService log, 
            ISettingsService settings,
            ArgumentsInputService argumentInput,
            SecretServiceManager secretServiceManager)
        {
            _log = log;
            _argumentInput = argumentInput;
            _settings = settings;
            _secretServiceManager = secretServiceManager;
        }

        public override async Task<CentralSslOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var path = await _argumentInput.
                GetString<CentralSslArguments>(args => args.CentralSslStore).
                Interactive(input, "Store path").
                WithDefault(CentralSsl.DefaultPath(_settings)).
                Required().
                Validate(x => Task.FromResult(x.ValidPath(_log)), "Invalid path").
                DefaultAsNull().
                GetValue();

            var password = await _argumentInput.
                GetProtectedString<CentralSslArguments>(args => args.PfxPassword, true).
                WithDefault(CentralSsl.DefaultPassword(_settings).Protect()).
                Interactive(input, "Password to use for the .pfx files").
                DefaultAsNull().
                GetValue();

            return Create(path, password);
        }

        public override async Task<CentralSslOptions?> Default()
        {
            var path = await _argumentInput.
                GetString<CentralSslArguments>(args => args.CentralSslStore).
                WithDefault(CentralSsl.DefaultPath(_settings)).
                Required().
                Validate(x => Task.FromResult(x.ValidPath(_log)), "Invalid path").
                DefaultAsNull().
                GetValue();

            var password = await _argumentInput.
                GetProtectedString<CentralSslArguments>(args => args.PfxPassword).
                WithDefault(CentralSsl.DefaultPassword(_settings).Protect()).
                DefaultAsNull().
                GetValue();
           
            return Create(path, password);
        }

        private static CentralSslOptions Create(string? path, ProtectedString? password)
        {
            var ret = new CentralSslOptions
            {
                KeepExisting = false,
                PfxPassword = password,
                Path = path
            };
            return ret;
        }
    }
}
