using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(CloudDnsOptions))]
    internal partial class CloudDnsJson : JsonSerializerContext
    {
        public CloudDnsJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class CloudDnsOptions : ValidationPluginOptions
    {
        public string? ServiceAccountKeyPath { get; set; }
        public string? ProjectId { get; set; }
    }
}
