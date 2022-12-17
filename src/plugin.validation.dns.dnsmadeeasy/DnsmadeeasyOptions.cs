using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("13993334-2d74-4ff6-801b-833b99bf231d")]
    internal class DnsMadeEasyOptions : ValidationPluginOptions<DnsMadeEasyDnsValidation>
    {
        public override string Name => "DnsMadeEasy";
        public override string Description => "Create verification records in DnsMadeEasy DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        public ProtectedString? ApiKey { get; set; }
        public ProtectedString? ApiSecret { get; set; }
    }
}
