using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("73af2c2e-4cf1-4198-a4c8-1129003cfb75")]
    public class CloudflareOptions : ValidationPluginOptions<Cloudflare>
    {
        public override string Name => "Cloudflare";
        public override string Description => "Create verification records in Cloudflare DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public ProtectedString? ApiToken { get; set; }
    }
}