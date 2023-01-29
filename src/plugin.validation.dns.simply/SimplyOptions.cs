using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(SimplyOptions))]
    internal partial class SimplyJson : JsonSerializerContext
    {
        public SimplyJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class SimplyOptions : ValidationPluginOptions
    {
        public string? Account { get; set; }
        public ProtectedString? ApiKey { get; set; }
    }
}
