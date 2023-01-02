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
    internal class NS1OptionsFactory : ValidationPluginOptionsFactory<NS1DnsValidation, NS1Options>
    {
        private readonly ArgumentsInputService _arguments;

        public NS1OptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<NS1Arguments>(a => a.ApiKey).
            Required();

        public override async Task<NS1Options?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new NS1Options()
            {
                ApiKey = await ApiKey.Interactive(input, "API key").GetValue(),
            };
        }

        public override async Task<NS1Options?> Default(Target target)
        {
            return new NS1Options()
            {
                ApiKey = await ApiKey.GetValue(),
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
