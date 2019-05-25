using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Properties;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptionsFactory : StorePluginOptionsFactory<PemFiles, PemFilesOptions>
    {
        public PemFilesOptionsFactory(ILogService log) : base(log) { }

        public override PemFilesOptions Aquire(IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<PemFilesArguments>();
            var path = args.PemFilesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Settings.Default.DefaultPemFilesPath;
            }
            while (string.IsNullOrWhiteSpace(path) || !path.ValidPath(_log))
            {
                path = input.RequestString("Path to folder where .pem files are stored");
            }
            return Create(path);
        }

        public override PemFilesOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<PemFilesArguments>();
            var path = Settings.Default.DefaultPemFilesPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = arguments.TryGetRequiredArgument(nameof(args.PemFilesPath), args.PemFilesPath);
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
            if (!string.Equals(path, Settings.Default.DefaultPemFilesPath, StringComparison.CurrentCultureIgnoreCase))
            {
                ret.Path = path;
            }
            return ret;
        }
    }

}