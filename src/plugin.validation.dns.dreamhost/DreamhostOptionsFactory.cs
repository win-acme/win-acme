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
    internal class DreamhostOptionsFactory : ValidationPluginOptionsFactory<DreamhostDnsValidation, DreamhostOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DreamhostOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString> ApiKey => _arguments.
            GetProtectedString<DreamhostArguments>(a => a.ApiKey).
            Required();

        public override async Task<DreamhostOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new DreamhostOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue()
            };
        }

        public override async Task<DreamhostOptions> Default(Target target)
        {
            return new DreamhostOptions()
            {
                ApiKey = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
