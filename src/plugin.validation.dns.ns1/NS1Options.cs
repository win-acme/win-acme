using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(NS1Options))]
    internal partial class NS1Json : JsonSerializerContext
    {
        public NS1Json(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class NS1Options : ValidationPluginOptions<NS1DnsValidation>
    {
        [JsonPropertyName("APIKeySafe")]
        public ProtectedString? ApiKey { get; set; }
    }
}
