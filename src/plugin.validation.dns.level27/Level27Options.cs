using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(Level27Options))]
    internal partial class Level27Json : JsonSerializerContext
    {
        public Level27Json(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class Level27Options : ValidationPluginOptions
    {
        public ProtectedString? ApiKey { get; set; }

        public string? ApiUrl { get; set; }
    }
}
