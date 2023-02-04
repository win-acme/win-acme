using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : PluginOptionsFactory<SelfHostingOptions>
    {
        private readonly ArgumentsInputService _arguments;

        private ArgumentResult<int?> ValidationPort =>
            _arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort);

        private ArgumentResult<string?> ValidationProtocol =>
            _arguments.GetString<SelfHostingArguments>(x => x.ValidationProtocol);

        public SelfHostingOptionsFactory(ArgumentsInputService arguments) => 
            _arguments = arguments;

        public override async Task<SelfHostingOptions?> Default()
        {
            return new SelfHostingOptions()
            {
                Port = await ValidationPort.GetValue(),
                Https = (await ValidationProtocol.GetValue())?.ToLower() == "https" ? true : null
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(SelfHostingOptions options)
        {
            yield return (ValidationPort.Meta, options.Port);
            if (options.Https == true) 
            {
                yield return (ValidationProtocol.Meta, "https");
            }
        }
    }
}