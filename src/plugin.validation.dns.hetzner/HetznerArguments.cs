using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class HetznerArguments : BaseArguments
    {
        public override string Name => "Hetzner";
        public override string Group => "Validation";
        public override string Condition => "--validation hetzner";

        [CommandLine(Description = "API Token for Hetzner.", Secret = true)]
        public string? HetznerApiToken { get; set; }

        [CommandLine(Description = "OPTIONAL: ID of zone the record is associated with.")]
        public string? HetznerZoneId { get; set; }
    }
}
