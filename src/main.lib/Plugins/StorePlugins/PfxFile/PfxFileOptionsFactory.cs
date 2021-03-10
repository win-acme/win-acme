using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PfxFileOptionsFactory : StorePluginOptionsFactory<PfxFile, PfxFileOptions>
    {
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;
        private readonly ISettingsService _settings;

        public PfxFileOptionsFactory(ILogService log, ISettingsService settings, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        public override async Task<PfxFileOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<PfxFileArguments>();

            // Get path from command line, default setting or user input
            var path = args?.PfxFilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _settings.Store.PfxFile?.DefaultPath;
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = await input.RequestString("Path to folder to store the .pfx file");
            }

            // Get password from command line, default setting or user input
            var password = args?.PfxPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                password = _settings.Store.PfxFile?.DefaultPassword;
            }
            if (string.IsNullOrEmpty(password))
            {
                password = await input.ReadPassword("Password to use for the .pfx files or <Enter> for none");
            }
            return Create(path, password);
        }

        public override async Task<PfxFileOptions?> Default()
        {
            var args = _arguments.GetArguments<PfxFileArguments>();
            var path = _settings.Store.PfxFile?.DefaultPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _arguments.TryGetRequiredArgument(nameof(args.PfxFilePath), args?.PfxFilePath);
            }

            var password = _settings.Store.PfxFile?.DefaultPassword;
            if (!string.IsNullOrWhiteSpace(args?.PfxPassword))
            {
                password = args.PfxPassword;
            }

            if (path != null && path.ValidPath(_log))
            {
                return Create(path, password);
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }

        private PfxFileOptions Create(string path, string? password)
        {
            var ret = new PfxFileOptions();
            if (!string.IsNullOrWhiteSpace(password) && 
                !string.Equals(password, _settings.Store.PfxFile?.DefaultPassword))
            {
                ret.PfxPassword = new ProtectedString(password);
            }
            if (!string.Equals(path, _settings.Store.PfxFile?.DefaultPath, StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }
}
