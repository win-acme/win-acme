using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("4e5dc595-45c7-4461-929a-8f96a0c96b3d")]
    internal sealed class Route53Options : ValidationPluginOptions<Route53>
    {
        public override string Name { get; } = "Route53";
        public override string Description { get; } = "Create verification records in AWS Route 53";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;
        public string? IAMRole { get; set; }
        public string? AccessKeyId { get; set; }

        [JsonProperty(propertyName: "SecretAccessKeySafe")]
        public ProtectedString? SecretAccessKey { get; set; }
    }
}