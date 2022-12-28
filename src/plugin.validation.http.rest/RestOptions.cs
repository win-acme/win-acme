using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [JsonSerializable(typeof(RestOptions))]
    internal partial class RestJson : JsonSerializerContext {}

    internal sealed class RestOptions : ValidationPluginOptions<Rest>
    {
        public override string Name => "Rest";
        public override string Description => "Send verification files to the server by issuing HTTP REST-style requests";
        public ProtectedString? SecurityToken { get; set; }
        public bool? UseHttps { get; set; }
    }
}
