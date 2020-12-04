using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class TransIpOptionsFactory : ValidationPluginOptionsFactory<TransIp, TransIpOptions>
    {
        private readonly IArgumentsService _arguments;

        public TransIpOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<TransIpOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<TransIpArguments>();
            var options = new TransIpOptions()
            {
                Login = await _arguments.TryGetArgument(args.Login, input, "User name for the control panel"),
                PrivateKey = new ProtectedString(await _arguments.TryGetArgument(args.PrivateKey, input, "Private key for the API, generated in the control panel", multiline: true))
            };
            return options;
        }

        public override Task<TransIpOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<TransIpArguments>();
            var ret = new TransIpOptions
            {
                PrivateKey = new ProtectedString(_arguments.TryGetRequiredArgument("transip-privatekey", args.PrivateKey)),
                Login = _arguments.TryGetRequiredArgument("transip-login", args.Login)
            };
            return Task.FromResult(ret);
        }

        public override bool CanValidate(Target target) => true;
    }
}
