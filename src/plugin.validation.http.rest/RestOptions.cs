using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [JsonSerializable(typeof(RestOptions))]
    internal partial class RestJson : JsonSerializerContext
    {
        public RestJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class RestOptions : ValidationPluginOptions<Rest>
    {
        public ProtectedString? SecurityToken { get; set; }
        public bool? UseHttps { get; set; }
    }
}
