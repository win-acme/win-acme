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
    /// Domeneshop DNS validation
    /// </summary>
    internal class DomeneshopOptionsFactory : ValidationPluginOptionsFactory<DomeneshopDnsValidation, DomeneshopOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DomeneshopOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ClientId => _arguments.
            GetProtectedString<DomeneshopArguments>(a => a.ClientId).
            Required();

        private ArgumentResult<ProtectedString?> ClientSecret => _arguments.
            GetProtectedString<DomeneshopArguments>(a => a.ClientSecret);

        public override async Task<DomeneshopOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new DomeneshopOptions()
            {
                ClientId = await ClientId.Interactive(input).GetValue(),
                ClientSecret = await ClientSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<DomeneshopOptions?> Default(Target target)
        {
            return new DomeneshopOptions()
            {
                ClientId = await ClientId.GetValue(),
                ClientSecret = await ClientSecret.GetValue(),
            };
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
