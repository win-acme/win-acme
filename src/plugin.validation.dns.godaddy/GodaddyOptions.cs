using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(GodaddyOptions))]
    internal partial class GodaddyJson : JsonSerializerContext { }

    internal class GodaddyOptions : ValidationPluginOptions<GodaddyDnsValidation>
    {
        public override string Name => "Godaddy";
        public override string Description => "Create verification records in Godaddy DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        [JsonPropertyName("SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
        public ProtectedString? ApiSecret { get; set; }
    }
}
