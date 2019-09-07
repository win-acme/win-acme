using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : ValidationPluginOptionsFactory<SelfHosting, SelfHostingOptions>
    {
        private readonly IArgumentsService _arguments;

        public SelfHostingOptionsFactory(IArgumentsService arguments) => _arguments = arguments;

        public override SelfHostingOptions Aquire(Target target, IInputService inputService, RunLevel runLevel) => Default(target);

        public override SelfHostingOptions Default(Target target)
        {
            var args = _arguments.GetArguments<SelfHostingArguments>();
            return new SelfHostingOptions()
            {
                Port = args.ValidationPort
            };
        }
    }
}