using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class DreamhostOptionsFactory : ValidationPluginOptionsFactory<DreamhostDnsValidation, DreamhostOptions>
    {
        public DreamhostOptionsFactory(ILogService log) : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType)
        {

        }

        public override DreamhostOptions Aquire(Target target, IArgumentsService arguments, IInputService input, RunLevel runLevel)
        {
            var args = arguments.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = arguments.TryGetArgument(args.ApiKey, input, "ApiKey", true),
            };
        }

        public override DreamhostOptions Default(Target target, IArgumentsService arguments)
        {
            var az = arguments.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = arguments.TryGetRequiredArgument(nameof(az.ApiKey), az.ApiKey),
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
