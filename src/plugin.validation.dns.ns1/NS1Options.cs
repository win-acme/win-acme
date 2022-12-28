using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(NS1Options))]
    internal partial class NS1Json : JsonSerializerContext { }

    internal class NS1Options : ValidationPluginOptions<NS1DnsValidation>
    {
        public override string Name => "NS1";
        public override string Description => "Create verification records in NS1 DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        [JsonPropertyName("APIKeySafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
