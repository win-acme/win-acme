using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class CloudDnsArgumentsProvider : BaseArgumentsProvider<CloudDnsArguments>
    {
        public override string Name { get; } = "Google Cloud DNS";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validationmode dns-01 --validation gcpdns";
        public override void Configure(FluentCommandLineParser<CloudDnsArguments> parser)
        {
            _ = parser.Setup(_ => _.ServiceAccountKey)
                .As("serviceaccountkey")
                .WithDescription("Service Account Key to authenticate with GCP");

            _ = parser.Setup(_ => _.ProjectId)
                .As("projectid")
                .WithDescription("Project ID that is hosting Cloud DNS.");
        }
    }
}