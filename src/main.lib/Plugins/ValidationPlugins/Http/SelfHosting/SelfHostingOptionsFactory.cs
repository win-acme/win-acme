using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : ValidationPluginOptionsFactory<SelfHosting, SelfHostingOptions>
    {
        private readonly IArgumentsService _arguments;
        private readonly UserRoleService _userRoleService;

        public SelfHostingOptionsFactory(IArgumentsService arguments, UserRoleService userRoleService)
        {
            _arguments = arguments;
            _userRoleService = userRoleService;
        }

        public override bool Disabled => SelfHosting.IsDisabled(_userRoleService);

        public override Task<SelfHostingOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel) => Default(target);

        public override Task<SelfHostingOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<SelfHostingArguments>();
            return Task.FromResult(new SelfHostingOptions()
            {
                Port = args.ValidationPort
            });
        }
    }
}