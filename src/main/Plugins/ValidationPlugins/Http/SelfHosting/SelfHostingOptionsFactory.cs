using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : ValidationPluginOptionsFactory<SelfHosting, SelfHostingOptions>
    {
        public SelfHostingOptionsFactory(ILogService log) : base(log) { }

        public override SelfHostingOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Default(target, arguments);
        }

        public override SelfHostingOptions Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<SelfHostingArguments>();
            return new SelfHostingOptions()
            {
                Port = args.ValidationPort
            };
        }
    }
}