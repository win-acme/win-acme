using PKISharp.WACS.Plugins.Base.Options;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    [JsonSerializable(typeof(CsrPluginOptions))]
    internal partial class WacsJson : JsonSerializerContext { }
}
