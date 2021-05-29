using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("966c4c3d-1572-44c7-9134-5e2bc8fa021d")]
    internal class GodaddyOptions : ValidationPluginOptions<GodaddyDnsValidation>
    {
        public override string Name => "Godaddy";
        public override string Description => "Create verification records in Godaddy DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
        public ProtectedString? ApiSecret { get; set; }
    }
}
