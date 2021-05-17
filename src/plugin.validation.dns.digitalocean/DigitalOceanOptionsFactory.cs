using System.Linq;
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
        private readonly ArgumentsInputService _arguments;

        public DigitalOceanOptionsFactory(ArgumentsInputService arguments) : 
            base(Dns01ChallengeValidationDetails.Dns01ChallengeType)
            => _arguments = arguments;

        private ArgumentResult<ProtectedString> ApiKey => _arguments.
            GetProtectedString<DigitalOceanArguments>(a => a.ApiToken).
            Required();

        public override async Task<DigitalOceanOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new DigitalOceanOptions
            {
                ApiToken = await ApiKey.Interactive(inputService).GetValue()
            };
        }

        public override async Task<DigitalOceanOptions> Default(Target target)
        {
            return new DigitalOceanOptions
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}