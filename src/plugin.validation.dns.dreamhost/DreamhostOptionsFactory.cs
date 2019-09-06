using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class DreamhostOptionsFactory : ValidationPluginOptionsFactory<DreamhostDnsValidation, DreamhostOptions>
    {
        private readonly IArgumentsService _arguments;

        public DreamhostOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType)
        {
            _arguments = arguments;
        }

        public override DreamhostOptions Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = new ProtectedString(_arguments.TryGetArgument(args.ApiKey, input, "ApiKey", true)),
            };
        }

        public override DreamhostOptions Default(Target target)
        {
            var az = _arguments.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(az.ApiKey), az.ApiKey)),
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
