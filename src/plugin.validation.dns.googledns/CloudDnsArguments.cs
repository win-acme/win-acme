using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class CloudDnsArguments : BaseArguments
    {
        public override string Name { get; } = "Google Cloud DNS";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validation gcpdns";

        [CommandLine(Description = "Service Account Key to authenticate with GCP")]
        public string? ServiceAccountKey { get; set; }

        [CommandLine(Description = "Project ID that is hosting Cloud DNS.")]
        public string? ProjectId { get; set; }
    }
}