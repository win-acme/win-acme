using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("3693c40c-7c2f-4b70-aead-27869d8cbdf3")]
    internal class SimplyOptions : ValidationPluginOptions<SimplyDnsValidation>
    {
        public override string Name => "Simply";

        public override string Description => "Create verification records in Simply DNS";

        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? Account { get; set; }

        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
