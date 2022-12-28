using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ManualOptions : ValidationPluginOptions<Manual>
    {
        public override string Name => "Manual";
        public override string Description => "Create verification records manually (auto-renew not possible)";
        public override string ChallengeType => Constants.Dns01ChallengeType;
    }
}
