using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class ManualOptions : ValidationPluginOptions<Manual>
    {
        public override string Name => "Manual";
        public override string Description => "Manually create record (for testing ONLY)";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }
    }
}
