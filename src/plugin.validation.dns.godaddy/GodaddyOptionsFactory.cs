using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class GodaddyOptionsFactory : ValidationPluginOptionsFactory<GodaddyDnsValidation, GodaddyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public GodaddyOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString> ApiKey => _arguments.
            GetProtectedString<GodaddyArguments>(a => a.ApiKey).
            Required();

        private ArgumentResult<ProtectedString> ApiSecret => _arguments.
            GetProtectedString<GodaddyArguments>(a => a.ApiSecret);

        public override async Task<GodaddyOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new GodaddyOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue(),
                ApiSecret = await ApiSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<GodaddyOptions> Default(Target target)
        {
            return new GodaddyOptions()
            {
                ApiKey = await ApiKey.GetValue(),
                ApiSecret = await ApiSecret.GetValue(),
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
