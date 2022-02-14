using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("C66CC8BE-3046-46C2-A0BA-EC4EC3E7FE96")]
    internal class NS1Options : ValidationPluginOptions<NS1DnsValidation>
    {
        public override string Name => "NS1";
        public override string Description => "Create verification records in NS1 DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        [JsonProperty(propertyName: "APIKeySafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
