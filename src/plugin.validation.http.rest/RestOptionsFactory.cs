using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// HTTP validation through REST endpoints on the server
    /// </summary>
    internal sealed class RestOptionsFactory : PluginOptionsFactory<RestOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public RestOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> SecurityToken => _arguments
            .GetProtectedString<RestArguments>(a => a.SecurityToken)
            .Required();
        private ArgumentResult<bool?> UseHttps => _arguments
            .GetBool<RestArguments>(a => a.UseHttps)
            .WithDefault(false)
            .Required();

        public override async Task<RestOptions?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new RestOptions()
            {
                SecurityToken = await SecurityToken.Interactive(inputService, "Security token").GetValue(),
                UseHttps = await UseHttps.Interactive(inputService, "Use HTTPS").GetValue(),
            };
        }

        public override async Task<RestOptions?> Default()
        {
            return new RestOptions()
            {
                SecurityToken = await SecurityToken.GetValue(),
                UseHttps = await UseHttps.GetValue(),
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(RestOptions options)
        {
            yield return (SecurityToken.Meta, options.SecurityToken);
            yield return (UseHttps.Meta, options.UseHttps);
        }
    }

}
