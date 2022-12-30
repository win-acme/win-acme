using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(CloudDnsOptions))]
    internal partial class CloudDnsJson : JsonSerializerContext
    {
        public CloudDnsJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class CloudDnsOptions : ValidationPluginOptions<CloudDns>
    {
        public override string Name => "GCPDns";
        public override string Description => "Create verification records in Google Cloud DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? ServiceAccountKeyPath { get; set; }

        public string? ProjectId { get; set; }
    }
}
