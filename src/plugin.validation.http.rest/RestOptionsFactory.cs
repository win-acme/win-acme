using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// HTTP validation through REST endpoints on the server
    /// </summary>
    internal sealed class RestOptionsFactory : ValidationPluginOptionsFactory<Rest, RestOptions>
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

        public override async Task<RestOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new RestOptions()
            {
                SecurityToken = await SecurityToken.Interactive(inputService, "Security token").GetValue(),
                UseHttps = await UseHttps.Interactive(inputService, "Use HTTPS").GetValue(),
            };
        }

        public override async Task<RestOptions?> Default(Target target)
        {
            return new RestOptions()
            {
                SecurityToken = await SecurityToken.GetValue(),
                UseHttps = await UseHttps.GetValue(),
            };
        }
    }

}
