using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class CloudflareOptionsFactory : PluginOptionsFactory<CloudflareOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public CloudflareOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<CloudflareArguments>(a => a.CloudflareApiToken).
            Required();

        public override async Task<CloudflareOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new CloudflareOptions
            {
                ApiToken = await ApiKey.Interactive(inputService, "Cloudflare API Token").GetValue()
            };
        }

        public override async Task<CloudflareOptions?> Default()
        {
            return new CloudflareOptions
            {
                ApiToken = await ApiKey.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(CloudflareOptions options)
        {
            yield return (ApiKey.Meta, options.ApiToken);
        }

    }
}
