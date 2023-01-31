using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(GodaddyOptions))]
    internal partial class GodaddyJson : JsonSerializerContext
    {
        public GodaddyJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class GodaddyOptions : ValidationPluginOptions
    {
        [JsonPropertyName("SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
        public ProtectedString? ApiSecret { get; set; }
    }
}
