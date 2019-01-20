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

        public override DreamhostOptions Aquire(Target target, IOptionsService options, IInputService input, RunLevel runLevel)
        {
            var arguments = options.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = options.TryGetOption(arguments.ApiKey, input, "ApiKey", true),
            };
        }

        public override DreamhostOptions Default(Target target, IOptionsService options)
        {
            var az = options.GetArguments<DreamhostArguments>();
            return new DreamhostOptions()
            {
                ApiKey = options.TryGetRequiredOption(nameof(az.ApiKey), az.ApiKey),
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
