using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptionsFactory : StorePluginOptionsFactory<PemFiles, PemFilesOptions>
    {
        private ILogService _log;
        private IArgumentsService _arguments;
        private ISettingsService _settings;

        public PemFilesOptionsFactory(ILogService log, ISettingsService settings, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        public override PemFilesOptions Aquire(IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<PemFilesArguments>();
            var path = args.PemFilesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _settings.DefaultPemFilesPath;
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = input.RequestString("Path to folder where .pem files are stored");
            }
            return Create(path);
        }

        public override PemFilesOptions Default()
        {
            var args = _arguments.GetArguments<PemFilesArguments>();
            var path = _settings.DefaultPemFilesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = _arguments.TryGetRequiredArgument(nameof(args.PemFilesPath), args.PemFilesPath);
            }
            if (path.ValidPath(_log))
            {
                return Create(path);
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }

        private PemFilesOptions Create(string path)
        {
            var ret = new PemFilesOptions();
            if (!string.Equals(path, _settings.DefaultPemFilesPath, StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }

}