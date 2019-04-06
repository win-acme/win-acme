using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("4e5dc595-45c7-4461-929a-8f96a0c96b3d")]
    internal sealed class Route53Options : ValidationPluginOptions<Route53>
    {
        public override string Name { get; } = "Route53";
        public override string Description { get; } = "Change records in Route53 DNS";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;
        public string AccessKeyId { get; set; }
        public string SecretAccessKeySafe { get; set; }

        [JsonIgnore]
        public string SecretAccessKey
        {
            get => SecretAccessKeySafe.Unprotect();
            set => SecretAccessKeySafe = value.Protect();
        }
    }
}