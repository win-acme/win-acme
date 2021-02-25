using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptionsFactory : StorePluginOptionsFactory<PemFiles, PemFilesOptions>
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;
        private readonly ISettingsService _settings;

        public PemFilesOptionsFactory(ILogService log, ISettingsService settings, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        public override async Task<PemFilesOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<PemFilesArguments>();
            var path = args?.PemFilesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = PemFiles.DefaultPath(_settings);
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = await input.RequestString("Path to folder where .pem files are stored");
            }

            // Get password from command line, default setting or user input
            var password = args?.PemPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                password = _settings.Store.PemFiles?.DefaultPassword;
            }
            if (string.IsNullOrEmpty(password))
            {
                password = await input.ReadPassword("Password to use for the private key .pem file or <Enter> for none");
            }
            return Create(path, password);
        }

        public override async Task<PemFilesOptions?> Default()
        {
            var args = _arguments.GetArguments<PemFilesArguments>();

            var password = _settings.Store.PemFiles?.DefaultPassword;
            if (!string.IsNullOrWhiteSpace(args?.PemPassword))
            {
                password = args.PemPassword;
            }

            var path = PemFiles.DefaultPath(_settings);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _arguments.TryGetRequiredArgument(nameof(args.PemFilesPath), args?.PemFilesPath);
            }
            if (path.ValidPath(_log))
            {
                return Create(path, password);
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }

        private PemFilesOptions Create(string path, string? password)
        {
            var ret = new PemFilesOptions();
            if (!string.IsNullOrWhiteSpace(password) &&
                !string.Equals(password, _settings.Store.PemFiles?.DefaultPassword))
            {
                ret.PemPassword = new ProtectedString(password);
            }
            if (!string.Equals(path, PemFiles.DefaultPath(_settings), StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }

}