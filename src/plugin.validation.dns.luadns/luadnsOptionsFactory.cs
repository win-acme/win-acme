using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class LUADNSOptionsFactory : ValidationPluginOptionsFactory<LUADNS, LUADNSOptions>
    {
        private readonly IArgumentsService _arguments;
        public LUADNSOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<LUADNSOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<LUADNSArguments>();
            var opts = new LUADNSOptions
            {
                Username = await _arguments.TryGetArgument(args.LUADNSUsername, input, "LUADNS Account username"),
                APIKey = new ProtectedString(await _arguments.TryGetArgument(args.LUADNSAPIKey, input, "LUADNS API key", true))
            };
            return opts;
        }

        public override Task<LUADNSOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<LUADNSArguments>();
            var opts = new LUADNSOptions
            {
                Username = _arguments.TryGetRequiredArgument(nameof(args.LUADNSUsername), args.LUADNSUsername),
                APIKey = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(args.LUADNSAPIKey), args.LUADNSAPIKey))
            };
            return Task.FromResult(opts);
        }

        public override bool CanValidate(Target target) => true;
    }
}
