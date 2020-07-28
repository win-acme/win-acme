using System.Threading.Tasks;
using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class DigitalOceanOptionsFactory : ValidationPluginOptionsFactory<DigitalOcean, DigitalOceanOptions>
    {
        private readonly IArgumentsService _arguments;

        public DigitalOceanOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType)
        {
            _arguments = arguments;
        }

        public override Task<DigitalOceanOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var arguments = _arguments.GetArguments<DigitalOceanArguments>();
            return Task.FromResult(new DigitalOceanOptions
            {
                ApiToken = new ProtectedString(arguments.ApiToken)
            });
        }

        public override Task<DigitalOceanOptions> Default(Target target)
        {
            var arguments = _arguments.GetArguments<DigitalOceanArguments>();
            return Task.FromResult(new DigitalOceanOptions
            {
                ApiToken = new ProtectedString(
                    _arguments.TryGetRequiredArgument(nameof(arguments.ApiToken), arguments.ApiToken))
            });
        }

        public override bool CanValidate(Target target) => true;
    }
}