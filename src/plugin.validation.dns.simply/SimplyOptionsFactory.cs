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
    /// Simply DNS validation
    /// </summary>
    internal class SimplyOptionsFactory : ValidationPluginOptionsFactory<SimplyDnsValidation, SimplyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public SimplyOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<string?> Account => _arguments.
            GetString<SimplyArguments>(a => a.Account).
            Required();

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<SimplyArguments>(a => a.ApiKey).
            Required();

        public override async Task<SimplyOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new SimplyOptions()
            {
                Account = await Account.Interactive(input).GetValue(),
                ApiKey = await ApiKey.Interactive(input).GetValue()
            };
        }

        public override async Task<SimplyOptions?> Default(Target target)
        {
            return new SimplyOptions()
            {
                Account = await Account.GetValue(),
                ApiKey = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
