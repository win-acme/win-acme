using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class GodaddyOptionsFactory : ValidationPluginOptionsFactory<GodaddyDnsValidation, GodaddyOptions>
    {
        private readonly IArgumentsService _arguments;

        public GodaddyOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<GodaddyOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<GodaddyArguments>();
            return new GodaddyOptions()
            {
                ApiKey = new ProtectedString(await _arguments.TryGetArgument(args.ApiKey, input, "ApiKey", true)),
            };
        }

        public override Task<GodaddyOptions> Default(Target target)
        {
            var az = _arguments.GetArguments<GodaddyArguments>();
            return Task.FromResult(new GodaddyOptions()
            {
                ApiKey = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(az.ApiKey), az.ApiKey)),
            });
        }

        public override bool CanValidate(Target target) => true;
    }
}
