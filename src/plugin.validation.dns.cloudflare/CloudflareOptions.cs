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

    public class CloudflareOptions : ValidationPluginOptions<Cloudflare>
    {
        public override string Name => "Cloudflare";
        public override string Description => "Create verification records in Cloudflare DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public ProtectedString? ApiToken { get; set; }
    }
}