using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class GandiArguments : BaseArguments
    {
        public override string Name => "Gandi";
        public override string Group => "Validation";
        public override string Condition => "--validation gandi";

        [CommandLine(Description = "Personal Access Token (or legacy API key) for Gandi LiveDNS.", Secret = true)]
        public string? GandiApiToken { get; set; }
    }
}
