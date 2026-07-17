using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(GandiOptions))]
    internal partial class GandiJson : JsonSerializerContext
    {
        public GandiJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    public class GandiOptions : ValidationPluginOptions
    {
        public ProtectedString? ApiToken { get; set; }
    }
}
