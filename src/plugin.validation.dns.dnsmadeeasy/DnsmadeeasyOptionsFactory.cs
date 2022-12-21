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
    /// DnsMadeEasy DNS validation
    /// </summary>
    internal class DnsMadeEasyOptionsFactory : ValidationPluginOptionsFactory<DnsMadeEasyDnsValidation, DnsMadeEasyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DnsMadeEasyOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<DnsMadeEasyArguments>(a => a.ApiKey).
            Required();

        private ArgumentResult<ProtectedString?> ApiSecret => _arguments.
            GetProtectedString<DnsMadeEasyArguments>(a => a.ApiSecret);

        public override async Task<DnsMadeEasyOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new DnsMadeEasyOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue(),
                ApiSecret = await ApiSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<DnsMadeEasyOptions?> Default(Target target)
        {
            return new DnsMadeEasyOptions()
            {
                ApiKey = await ApiKey.GetValue(),
                ApiSecret = await ApiSecret.GetValue(),
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
