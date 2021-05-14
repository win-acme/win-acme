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

        public CentralSslOptionsFactory(
            ILogService log, 
            ISettingsService settings,
            ArgumentsInputService argumentInput)
        {
            _log = log;
            _argumentInput = argumentInput;
            _settings = settings;
        }

        private ArgumentResult<ProtectedString?> PfxPassword => _argumentInput.
            GetProtectedString<CentralSslArguments>(args => args.PfxPassword, true).
            WithDefault(CentralSsl.DefaultPassword(_settings).Protect()).
            DefaultAsNull();

        private ArgumentResult<string?> Path => _argumentInput.
            GetString<CentralSslArguments>(args => args.CentralSslStore).
            WithDefault(CentralSsl.DefaultPath(_settings)).
            Required().
            Validate(x => Task.FromResult(x.ValidPath(_log)), "invalid path").
            DefaultAsNull();

        public override async Task<CentralSslOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var path = await Path.Interactive(input, "Store path").GetValue();
            var password = await PfxPassword.Interactive(input, "Password for the .pfx file").GetValue();
            return Create(path, password);
        }

        public override async Task<CentralSslOptions?> Default()
        {
            var path = await Path.GetValue();
            var password = await PfxPassword.GetValue();
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
