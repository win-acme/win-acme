using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class LuaDnsOptionsFactory : PluginOptionsFactory<LuaDnsOptions>
    {
        private readonly ArgumentsInputService _arguments;
        public LuaDnsOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<LuaDnsArguments>(a => a.LuaDnsAPIKey).
            Required();

        private ArgumentResult<string?> Username => _arguments.
            GetString<LuaDnsArguments>(a => a.LuaDnsUsername).
            Required();

        public override async Task<LuaDnsOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new LuaDnsOptions
            {
                Username = await Username.Interactive(input, "Username").GetValue(),
                APIKey = await ApiKey.Interactive(input, "API key").GetValue()
            };
        }

        public override async Task<LuaDnsOptions?> Default()
        {
            return new LuaDnsOptions
            {
                Username = await Username.GetValue(),
                APIKey = await ApiKey.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(LuaDnsOptions options)
        {
            yield return (Username.Meta, options.Username);
            yield return (ApiKey.Meta, options.APIKey);
        }
    }
}
