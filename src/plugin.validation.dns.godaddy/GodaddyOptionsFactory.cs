using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Godaddy DNS validation
    /// </summary>
    internal class GodaddyOptionsFactory : PluginOptionsFactory<GodaddyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public GodaddyOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<GodaddyArguments>(a => a.ApiKey).
            Required();

        private ArgumentResult<ProtectedString?> ApiSecret => _arguments.
            GetProtectedString<GodaddyArguments>(a => a.ApiSecret);

        public override async Task<GodaddyOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new GodaddyOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue(),
                ApiSecret = await ApiSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<GodaddyOptions?> Default()
        {
            return new GodaddyOptions()
            {
                ApiKey = await ApiKey.GetValue(),
                ApiSecret = await ApiSecret.GetValue(),
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(GodaddyOptions options)
        {
            yield return (ApiKey.Meta, options.ApiKey);
            yield return (ApiSecret.Meta, options.ApiSecret);
        }
    }
}
