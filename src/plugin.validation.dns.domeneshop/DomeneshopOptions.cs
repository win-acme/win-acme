using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(DomeneshopOptions))]
    internal partial class DomeneshopJson : JsonSerializerContext { }

    internal class DomeneshopOptions : ValidationPluginOptions<DomeneshopDnsValidation>
    {
        public override string Name => "Domeneshop";
        public override string Description => "Create verification records in Domeneshop DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        [JsonPropertyName("SecretSafe")]
        public ProtectedString? ClientId { get; set; }
        public ProtectedString? ClientSecret { get; set; }
    }
}
