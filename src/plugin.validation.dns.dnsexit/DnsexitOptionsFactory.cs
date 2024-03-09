using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Dnsexit DNS validation
    /// </summary>
    internal class DnsexitOptionsFactory : PluginOptionsFactory<DnsexitOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DnsexitOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<DnsexitArguments>(a => a.ApiKey).Required();


        public override async Task<DnsexitOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new DnsexitOptions() { ApiKey = await ApiKey.Interactive(input).GetValue() };
        }

        public override async Task<DnsexitOptions?> Default()
        {
            return new DnsexitOptions() { ApiKey = await ApiKey.GetValue()};
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(DnsexitOptions options)
        { 
            yield return (ApiKey.Meta, options.ApiKey);
        }
    }
}
