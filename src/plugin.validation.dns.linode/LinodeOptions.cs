using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("12fdc54c-be30-4458-8066-2c1c565fe2d9")]
    internal class LinodeOptions : ValidationPluginOptions<LinodeDnsValidation>
    {
        public override string Name => "Linode";
        public override string Description => "Create verification records in Linode DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        public ProtectedString? ApiToken { get; set; }
    }
}