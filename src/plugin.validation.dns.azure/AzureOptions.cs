using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    class AzureOptions : ValidationPluginOptions<Azure>
    {
        public override string Name => "Azure";
        public override string Description => "Change records in Azure DNS";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }

        public AzureDnsOptions AzureConfiguration { get; set; }
    }
}
