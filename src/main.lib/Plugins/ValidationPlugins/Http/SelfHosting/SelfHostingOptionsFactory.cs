using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptionsFactory : PluginOptionsFactory<SelfHostingOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public SelfHostingOptionsFactory(ArgumentsInputService arguments) => 
            _arguments = arguments;

        public override async Task<SelfHostingOptions?> Default()
        {
            return new SelfHostingOptions()
            {
                Port = await _arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort).GetValue(),
                Https = (await _arguments.GetString<SelfHostingArguments>(x => x.ValidationProtocol).GetValue())?.ToLower() == "https" ? true : null
            };
        }

        public override IEnumerable<string> Describe(SelfHostingOptions options)
        {
            if (options.Https == true)
            {
                yield return $"{_arguments.GetString<SelfHostingArguments>(x => x.ValidationProtocol).ArgumentName} https";
            }
            if (options.Port != null)
            {
                yield return $"{_arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort).ArgumentName} {options.Port}";
            }
        }
    }
}