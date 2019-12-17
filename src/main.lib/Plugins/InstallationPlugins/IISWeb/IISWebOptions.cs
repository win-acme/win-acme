using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("ea6a5be3-f8de-4d27-a6bd-750b619b2ee2")]
    internal class IISWebOptions : InstallationPluginOptions<IISWeb>
    {
        public long? SiteId { get; set; }
        public string? NewBindingIp { get; set; }
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
            if (sslPort.HasValue && 
                sslPort > 0 && 
                sslPort < 65536 && 
                sslPort != IISClient.DefaultBindingPort)
            {
                NewBindingPort = sslPort;
            }
        }
    }
}
