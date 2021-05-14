using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptionsFactory : StorePluginOptionsFactory<PemFiles, PemFilesOptions>
    {
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;
        private readonly ISettingsService _settings;

        public PemFilesOptionsFactory(
            ILogService log, 
            ISettingsService settings,
            ArgumentsInputService arguments)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        private ArgumentResult<ProtectedString?> Password => _arguments.
            GetProtectedString<PemFilesArguments>(args => args.PemPassword, true).
            WithDefault(PemFiles.DefaultPassword(_settings).Protect()).
            DefaultAsNull();

        private ArgumentResult<string?> Path => _arguments.
            GetString<PemFilesArguments>(args => args.PemFilesPath).
            WithDefault(PemFiles.DefaultPath(_settings)).
            Required().
            Validate(x => Task.FromResult(x.ValidPath(_log)), "invalid path").
            DefaultAsNull();

        public override async Task<PemFilesOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var path = await Path.Interactive(input, "File path").GetValue();
            var password = await Password.Interactive(input).GetValue();
            return Create(path, password);
        }

        public override async Task<PemFilesOptions?> Default()
        {
            var path = await Path.GetValue();
            var password = await Password.GetValue();
            return Create(path, password);
        }

        private static PemFilesOptions Create(string? path, ProtectedString? password)
        {
            return new PemFilesOptions
            {
                PemPassword = password,
                Path = path
            };
        }
    }

}