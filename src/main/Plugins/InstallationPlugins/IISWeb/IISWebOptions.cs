using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISWebOptions : InstallationPluginOptions<IISWeb>
    {
        public long? SiteId { get; set; }
        public string NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        public override string Name => "IIS";
        public override string Description => "Create or update https bindings in IIS";

        public IISWebOptions() { }
        public IISWebOptions(IISWebArguments args)
        {
            var sslIp = args.SSLIPAddress;
            if (!string.IsNullOrEmpty(sslIp) && sslIp != IISClient.DefaultBindingIp)
            {
                NewBindingIp = sslIp;
            }
            var sslPort = args.SSLPort;
            if (sslPort != IISClient.DefaultBindingPort)
            {
                NewBindingPort = sslPort;
            }
        }
    }
}
