using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class LuaDnsOptionsFactory : ValidationPluginOptionsFactory<LuaDns, LuaDnsOptions>
    {
        private readonly IArgumentsService _arguments;
        public LuaDnsOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<LuaDnsOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<LuaDnsArguments>();
            var opts = new LuaDnsOptions
            {
                Username = await _arguments.TryGetArgument(args.LuaDnsUsername, input, "LuaDns Account username"),
                APIKey = new ProtectedString(await _arguments.TryGetArgument(args.LuaDnsAPIKey, input, "LuaDns API key", true))
            };
            return opts;
        }

        public override Task<LuaDnsOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<LuaDnsArguments>();
            var opts = new LuaDnsOptions
            {
                Username = _arguments.TryGetRequiredArgument(nameof(args.LuaDnsUsername), args.LuaDnsUsername),
                APIKey = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(args.LuaDnsAPIKey), args.LuaDnsAPIKey))
            };
            return Task.FromResult(opts);
        }

        public override bool CanValidate(Target target) => true;
    }
}
