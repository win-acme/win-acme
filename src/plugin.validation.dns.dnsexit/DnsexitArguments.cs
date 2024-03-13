using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class DnsexitArguments : BaseArguments
    {
        public override string Name => "DnsExit";
        public override string Group => "Validation";
        public override string Condition => "--validation dnsexit";

        [CommandLine(Description = "DnsExit API key.", Secret = true)]
        public string? ApiKey { get; set; }
    }
}