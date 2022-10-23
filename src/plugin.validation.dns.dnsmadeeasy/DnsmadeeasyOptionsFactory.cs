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
    /// Dnsmadeeasy DNS validation
    /// </summary>
    internal class DnsmadeeasyOptionsFactory : ValidationPluginOptionsFactory<DnsmadeeasyDnsValidation, DnsmadeeasyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DnsmadeeasyOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<DnsmadeeasyArguments>(a => a.ApiKey).
            Required();

        private ArgumentResult<ProtectedString?> ApiSecret => _arguments.
            GetProtectedString<DnsmadeeasyArguments>(a => a.ApiSecret);

        public override async Task<DnsmadeeasyOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new DnsmadeeasyOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue(),
                ApiSecret = await ApiSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<DnsmadeeasyOptions?> Default(Target target)
        {
            return new DnsmadeeasyOptions()
            {
                ApiKey = await ApiKey.GetValue(),
                ApiSecret = await ApiSecret.GetValue(),
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
