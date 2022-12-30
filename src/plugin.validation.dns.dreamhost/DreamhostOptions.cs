using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(DreamhostOptions))]
    internal partial class DreamhostJson : JsonSerializerContext
    {
        public DreamhostJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class DreamhostOptions : ValidationPluginOptions<DreamhostDnsValidation>
    {
        public override string Name => "Dreamhost";

        public override string Description => "Create verification records in Dreamhost DNS";

        public override string ChallengeType => Constants.Dns01ChallengeType;

        [JsonPropertyName("SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
