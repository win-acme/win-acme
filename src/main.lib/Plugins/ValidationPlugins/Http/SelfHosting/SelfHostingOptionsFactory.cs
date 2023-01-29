using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
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
    }
}