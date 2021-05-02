using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class AcmeArguments : BaseArguments
    {
        public override string Name => "AcmeDns";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation acme-dns";

        [CommandLine(Description = "Root URI of the acme-dns service")]
        public string? AcmeDnsServer { get; set; }
    }
}
