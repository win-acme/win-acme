using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("B61505E9-1709-43FD-996F-C74C3686286C")]
    internal class CloudDnsOptions : ValidationPluginOptions<CloudDns>
    {
        public override string Name => "GCPDns";
        public override string Description => "Create verification records in Google Cloud DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? ServiceAccountKeyPath { get; set; }

        public string? ProjectId { get; set; }
    }
}
