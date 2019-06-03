using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("6f75e466-29f3-458b-89ea-71d8cc4af3c9")]
    class SelfDNSOptions : ValidationPluginOptions<SelfDNS>
    {
        public override string Name => "SelfDNS";
        public override string Description => "Selfhost Temporary DNS Server";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }
    }
}
