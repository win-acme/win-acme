using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(LuaDnsOptions))]
    internal partial class LuaDnsJson : JsonSerializerContext
    {
        public LuaDnsJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class LuaDnsOptions : ValidationPluginOptions<LuaDns>
    {
        public string? Username { get; set; }
        [JsonPropertyName("APIKeySafe")]
        public ProtectedString? APIKey { get; set; }
    }
}