using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(CloudflareOptions))]
    internal partial class CloudflareJson : JsonSerializerContext
    {
        public CloudflareJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    public class CloudflareOptions : ValidationPluginOptions
    {
        public ProtectedString? ApiToken { get; set; }
    }
}