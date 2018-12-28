using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISWebOptions : InstallationPluginOptions<IISWeb>
    {
        public long? SiteId { get; set; }
        public string NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        public IISWebOptions() { }
        public IISWebOptions(Options options)
        {
            var sslIp =options.SSLIPAddress;
            if (!string.IsNullOrEmpty(sslIp) && sslIp != IISClient.DefaultBindingIp)
            {
                NewBindingIp = sslIp;
            }
            var sslPort = options.SSLPort;
            if (sslPort != IISClient.DefaultBindingPort)
            {
                NewBindingPort = sslPort;
            }
        }
    }
}
