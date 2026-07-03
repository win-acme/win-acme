using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Level27OptionsFactory : PluginOptionsFactory<Level27Options>
    {
        private readonly ArgumentsInputService _arguments;

        public Level27OptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments
            .GetProtectedString<Level27Arguments>(a => a.Level27ApiKey)
            .Required();

        private ArgumentResult<string?> ApiUrl => _arguments
            .GetString<Level27Arguments>(a => a.Level27ApiUrl)
            .DefaultAsNull();

        public override async Task<Level27Options?> Aquire(IInputService inputService, RunLevel runLevel)
        {
            return new Level27Options
            {
                ApiKey = await ApiKey.Interactive(inputService, "Level27 API key").GetValue(),
                ApiUrl = await ApiUrl.Interactive(inputService, "Level27 API endpoint").GetValue()
            };
        }

        public override async Task<Level27Options?> Default()
        {
            return new Level27Options
            {
                ApiKey = await ApiKey.GetValue(),
                ApiUrl = await ApiUrl.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(Level27Options options)
        {
            yield return (ApiKey.Meta, options.ApiKey);
            yield return (ApiUrl.Meta, options.ApiUrl);
        }
    }
}
