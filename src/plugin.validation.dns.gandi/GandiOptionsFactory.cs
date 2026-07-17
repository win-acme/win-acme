using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class GandiOptionsFactory : PluginOptionsFactory<GandiOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public GandiOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiToken => _arguments.
            GetProtectedString<GandiArguments>(a => a.GandiApiToken).
            Required();

        public override async Task<GandiOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new GandiOptions
            {
                ApiToken = await ApiToken.Interactive(inputService, "Gandi API Token").GetValue()
            };
        }

        public override async Task<GandiOptions?> Default()
        {
            return new GandiOptions
            {
                ApiToken = await ApiToken.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(GandiOptions options)
        {
            yield return (ApiToken.Meta, options.ApiToken);
        }
    }
}
