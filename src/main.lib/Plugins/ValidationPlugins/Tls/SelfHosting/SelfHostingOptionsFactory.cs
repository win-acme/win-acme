using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Tls
{
    internal class SelfHostingOptionsFactory : PluginOptionsFactory<SelfHostingOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public SelfHostingOptionsFactory(ArgumentsInputService arguments)
            => _arguments = arguments;

        public override int Order => 100;

        public override async Task<SelfHostingOptions?> Default()
        {
            return new SelfHostingOptions()
            {
                Port = await _arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort).GetValue(),
            };
        }

        public override IEnumerable<string> Describe(SelfHostingOptions options)
        {
            if (options.Port != null)
            {
                yield return $"{_arguments.GetInt<SelfHostingArguments>(x => x.ValidationPort).ArgumentName} {options.Port}";
            }
        }
    }
}