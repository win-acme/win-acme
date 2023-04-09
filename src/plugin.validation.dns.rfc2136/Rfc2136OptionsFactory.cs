using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Rfc2136OptionsFactory : PluginOptionsFactory<Rfc2136Options>
    {
        private readonly ArgumentsInputService _arguments;
        public Rfc2136OptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;

        private ArgumentResult<string?> ServerHost => _arguments.
            GetString<Rfc2136Arguments>(a => a.ServerHost).
            Required();

        private ArgumentResult<int?> ServerPort => _arguments.
            GetInt<Rfc2136Arguments>(a => a.ServerPort).
            WithDefault(53).
            DefaultAsNull();

        private ArgumentResult<string?> TsigKeyName => _arguments.
            GetString<Rfc2136Arguments>(a => a.TsigKeyName).
            Required();

        private ArgumentResult<ProtectedString?> TsigKeySecret => _arguments.
            GetProtectedString<Rfc2136Arguments>(a => a.TsigKeySecret).
            Required();

        private ArgumentResult<string?> TsigKeyAlgorithm => _arguments.
            GetString<Rfc2136Arguments>(a => a.TsigKeyAlgorithm).
            Required();

        public override async Task<Rfc2136Options?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new Rfc2136Options
            {
                ServerHost = await ServerHost.Interactive(input).GetValue(),
                ServerPort = await ServerPort.Interactive(input).GetValue(),
                TsigKeyName = await TsigKeyName.Interactive(input).GetValue(),
                TsigKeySecret = await TsigKeySecret.Interactive(input).GetValue(),
                TsigKeyAlgorithm = await TsigKeyAlgorithm.Interactive(input).GetValue()
            };
        }

        public override async Task<Rfc2136Options?> Default()
        {
            return new Rfc2136Options
            {
                ServerHost = await ServerHost.GetValue(),
                ServerPort = await ServerPort.GetValue(),
                TsigKeyName = await TsigKeyName.GetValue(),
                TsigKeySecret = await TsigKeySecret.GetValue(),
                TsigKeyAlgorithm = await TsigKeyAlgorithm.GetValue()
            };
        }

        public override IEnumerable<(CommandLineAttribute, object?)> Describe(Rfc2136Options options)
        {
            yield return (ServerHost.Meta, options.ServerHost);
            yield return (ServerPort.Meta, options.ServerPort);
            yield return (TsigKeyName.Meta, options.TsigKeyName);
            yield return (TsigKeySecret.Meta, options.TsigKeySecret);
            yield return (TsigKeyAlgorithm.Meta, options.TsigKeyAlgorithm);
        }
    }
}
