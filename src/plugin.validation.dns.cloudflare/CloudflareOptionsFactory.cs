using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class CloudflareOptionsFactory : ValidationPluginOptionsFactory<Cloudflare, CloudflareOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public CloudflareOptionsFactory(ArgumentsInputService arguments) : 
            base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => 
            _arguments = arguments;

        private ArgumentResult<ProtectedString> ApiKey => _arguments.
            GetProtectedString<CloudflareArguments>(a => a.CloudflareApiToken).
            Required();

        public override async Task<CloudflareOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new CloudflareOptions
            {
                ApiToken = await ApiKey.Interactive(inputService, "Cloudflare API Token").GetValue()
            };
        }

        public override async Task<CloudflareOptions> Default(Target target)
        {
            return new CloudflareOptions
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
