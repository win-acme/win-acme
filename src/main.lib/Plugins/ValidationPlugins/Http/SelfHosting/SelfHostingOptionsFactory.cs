using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : ValidationPluginOptionsFactory<SelfHosting, SelfHostingOptions>
    {
        private readonly ArgumentsInputService _arguments;
        private readonly IUserRoleService _userRoleService;

        public SelfHostingOptionsFactory(ArgumentsInputService arguments, IUserRoleService userRoleService)
        {
            _arguments = arguments;
            _userRoleService = userRoleService;
        }

        public override (bool, string?) Disabled => SelfHosting.IsDisabled(_userRoleService);

        public override Task<SelfHostingOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel) => Default(target);

        public override async Task<SelfHostingOptions?> Default(Target target)
        {
            return new SelfHostingOptions()
            {
                Port = await _arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort).GetValue(),
                Https = (await _arguments.GetString<SelfHostingArguments>(x => x.ValidationProtocol).GetValue())?.ToLower() == "https" ? true : null
            };
        }
    }
}