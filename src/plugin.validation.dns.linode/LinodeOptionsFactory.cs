using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class LinodeOptionsFactory : PluginOptionsFactory<LinodeOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public LinodeOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<LinodeArguments>(a => a.ApiToken).
            Required();

        public override async Task<LinodeOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new LinodeOptions()
            {
                ApiToken = await ApiKey.Interactive(input).GetValue()
            };
        }

        public override async Task<LinodeOptions?> Default()
        {
            return new LinodeOptions()
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(LinodeOptions options)
        {
            yield return (ApiKey.Meta, options.ApiToken);
        }
    }
}
