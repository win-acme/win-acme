using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class DigitalOceanOptionsFactory : PluginOptionsFactory<DigitalOceanOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DigitalOceanOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<DigitalOceanArguments>(a => a.ApiToken).
            Required();

        public override async Task<DigitalOceanOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new DigitalOceanOptions
            {
                ApiToken = await ApiKey.Interactive(inputService).GetValue()
            };
        }

        public override async Task<DigitalOceanOptions?> Default()
        {
            return new DigitalOceanOptions
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(DigitalOceanOptions options)
        {
            yield return (ApiKey.Meta, options.ApiToken);
        }
    }
}