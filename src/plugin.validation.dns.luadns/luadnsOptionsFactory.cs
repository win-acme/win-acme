using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class LuaDnsOptionsFactory : ValidationPluginOptionsFactory<LuaDns, LuaDnsOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public LuaDnsOptionsFactory(ArgumentsInputService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        private ArgumentResult<ProtectedString> ApiKey => _arguments.
            GetProtectedString<LuaDnsArguments>(a => a.LuaDnsAPIKey).
            Required();

        private ArgumentResult<string> Username => _arguments.
            GetString<LuaDnsArguments>(a => a.LuaDnsUsername).
            Required();

        public override async Task<LuaDnsOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new LuaDnsOptions
            {
                Username = await Username.Interactive(input, "Username").GetValue(),
                APIKey = await ApiKey.Interactive(input, "API key").GetValue()
            };
        }

        public override async Task<LuaDnsOptions> Default(Target target)
        {
            return new LuaDnsOptions
            {
                Username = await Username.GetValue(),
                APIKey = await ApiKey.GetValue()
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
