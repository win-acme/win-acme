using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("2bfb3ef8-64b8-47f1-8185-ea427b793c1a")]
    internal class DreamhostOptions : ValidationPluginOptions<DreamhostDnsValidation>
    {
        public override string Name => "Dreamhost";

        public override string Description => "Create verification records in Dreamhost DNS";

        public override string ChallengeType => Constants.Dns01ChallengeType;

        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
