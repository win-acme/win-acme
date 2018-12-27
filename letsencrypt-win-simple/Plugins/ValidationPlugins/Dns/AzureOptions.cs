using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class AzureOptions : ValidationPluginOptions<Azure>
    {
        public override string Name => "Azure";
        public override string Description => "Change records in Azure DNS";

        public AzureDnsOptions AzureConfiguration { get; set; }
    }
}
