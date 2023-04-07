using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(Rfc2136Options))]
    internal partial class Rfc2136Json : JsonSerializerContext
    {
        public Rfc2136Json(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class Rfc2136Options : ValidationPluginOptions
    {
        public string? ServerHost { get; set; }
        public int? ServerPort { get; set; }
        public string? TsigKeyName { get; set; }
        public ProtectedString? TsigKeySecret { get; set; }
        public string? TsigKeyAlgorithm { get; set; }
    }
}