using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("1a87d670-3fa3-4a2a-bb10-491d48feb5db")]
    internal class DigitalOceanOptions : ValidationPluginOptions<DigitalOcean>
    {
        public override string Name => "DigitalOcean";
        public override string Description => "Create verification records on DigitalOcean";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        public ProtectedString? ApiToken { get; set; }
    }
}