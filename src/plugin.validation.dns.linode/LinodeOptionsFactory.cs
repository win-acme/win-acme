using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class LinodeOptionsFactory : ValidationPluginOptionsFactory<LinodeDnsValidation, LinodeOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public LinodeOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<LinodeArguments>(a => a.ApiToken).
            Required();

        public override async Task<LinodeOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new LinodeOptions()
            {
                ApiToken = await ApiKey.Interactive(input).GetValue()
            };
        }

        public override async Task<LinodeOptions?> Default(Target target)
        {
            return new LinodeOptions()
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
