using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(Route53Options))]
    internal partial class Route53Json : JsonSerializerContext
    {
        public Route53Json(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class Route53Options : ValidationPluginOptions
    {
        public string? IAMRole { get; set; }
        public string? AccessKeyId { get; set; }

        [JsonPropertyName("SecretAccessKeySafe")]
        public ProtectedString? SecretAccessKey { get; set; }
    }
}