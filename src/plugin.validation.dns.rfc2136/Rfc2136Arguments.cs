using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class Rfc2136Arguments : BaseArguments
    {
        public override string Name { get; } = "Rfc2136";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validation rfc2136";

        [CommandLine(Description = "DNS server host/ip")]
        public string? ServerHost { get; set; }

        [CommandLine(Description = "DNS server port")]
        public int? ServerPort { get; set; }

        [CommandLine(Description = "TSIG key name")]
        public string? TsigKeyName { get; set; }

        [CommandLine(Description = "TSIG key secret (Base64 encoded)", Secret = true)]
        public string? TsigKeySecret { get; set; }

        [CommandLine(Description = "TSIG key algorithm")]
        public string? TsigKeyAlgorithm { get; set; }
    }
}