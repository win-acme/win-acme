using PKISharp.WACS.Services;
using System.Net;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebArguments
    {
        public long? InstallationSiteId { get; set; }
        public string? SSLPort { get; set; }
        public string? SSLIPAddress { get; set; }

    }
}
