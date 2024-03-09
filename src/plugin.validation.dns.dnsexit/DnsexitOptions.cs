using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(DnsexitOptions))]
    internal partial class DnsexitJson : JsonSerializerContext
    {
        public DnsexitJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class DnsexitOptions : ValidationPluginOptions
    {
        public ProtectedString? ApiKey { get; set; }
    }
}
