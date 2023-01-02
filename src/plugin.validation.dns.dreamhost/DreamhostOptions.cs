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
        [JsonPropertyName("SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
