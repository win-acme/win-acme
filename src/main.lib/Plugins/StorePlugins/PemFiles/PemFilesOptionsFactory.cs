using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptionsFactory : PluginOptionsFactory<PemFilesOptions>
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
            WithDefault(_settings.Store.PemFiles.DefaultPassword.Protect()).
            DefaultAsNull();

        private ArgumentResult<string?> Path => _arguments.
            GetString<PemFilesArguments>(args => args.PemFilesPath).
            WithDefault(_settings.Store.PemFiles.DefaultPath).
            Required().
            Validate(x => Task.FromResult(x.ValidPath(_log)), "invalid path").
            DefaultAsNull();

        private ArgumentResult<string?> Name => _arguments.
            GetString<PemFilesArguments>(args => args.PemFilesName);

        public override async Task<PemFilesOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var path = await Path.Interactive(input, "File path").GetValue();
            var name = await Name.GetValue();
            var password = await Password.Interactive(input).GetValue();
            return Create(path, name, password);
        }

        public override async Task<PemFilesOptions?> Default()
        {
            var path = await Path.GetValue();
            var name = await Name.GetValue();
            var password = await Password.GetValue();
            return Create(path, name, password);
        }

        private static PemFilesOptions Create(
            string? path, 
            string? name,
            ProtectedString? password)
        {
            return new PemFilesOptions
            {
                PemPassword = password,
                Path = path,
                FileName = name
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(PemFilesOptions options)
        {
            yield return (Path.Meta, options.Path);
            yield return (Password.Meta, options.PemPassword);
        }
    }

}