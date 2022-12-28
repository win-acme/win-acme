using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(Route53Options))]
    internal partial class Route53Json : JsonSerializerContext { }

    internal sealed class Route53Options : ValidationPluginOptions<Route53>
    {
        public override string Name { get; } = "Route53";
        public override string Description { get; } = "Create verification records in AWS Route 53";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;
        public string? IAMRole { get; set; }
        public string? AccessKeyId { get; set; }

        [JsonPropertyName("SecretAccessKeySafe")]
        public ProtectedString? SecretAccessKey { get; set; }
    }
}