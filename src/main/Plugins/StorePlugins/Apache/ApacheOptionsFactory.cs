using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class ApacheOptionsFactory : StorePluginOptionsFactory<Apache, ApacheOptions>
    {
        public ApacheOptionsFactory(ILogService log) : base(log) { }

        public override ApacheOptions Aquire(IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<ApacheArguments>();
            var path = args.ApacheCertificatePath;
            while (!path.ValidPath(_log))
            {
                path = input.RequestString("Path to Apache certificate folder");
            }
            return Create(path);
        }

        public override ApacheOptions Default(IArgumentsService arguments)
        {
            var args = arguments.GetArguments<ApacheArguments>();
            var path = arguments.TryGetRequiredArgument(nameof(args.ApacheCertificatePath), args.ApacheCertificatePath);
            if (path.ValidPath(_log))
            {
                return Create(path);
            }
            else
            {
                throw new Exception("Invalid path specified");
            }
        }

        private ApacheOptions Create(string path)
        {
            var ret = new ApacheOptions
            {
                Path = path
            };
            return ret;
        }
    }
}
