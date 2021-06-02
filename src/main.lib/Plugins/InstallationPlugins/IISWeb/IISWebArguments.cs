using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Services;
using System.Net;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebArguments : BaseArguments
    {
        private const string SslPortParameterName = "sslport";
        private const string SslIpParameterName = "sslipaddress";

        public override string Name => "IIS Web plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iis";

        [CommandLine(Description = "Specify site to install new bindings to. Defaults to the source if that is an IIS site.")]
        public long? InstallationSiteId { get; set; }

        [CommandLine(Name = SslPortParameterName, Description = "Port number to use for newly created HTTPS bindings. Defaults to " + IISClient.DefaultBindingPortFormat + ".")]
        public int? SSLPort { get; set; }

        [CommandLine(Name = SslIpParameterName, Description = "IP address to use for newly created HTTPS bindings. Defaults to " + IISClient.DefaultBindingIp + ".")]
        public string? SSLIPAddress { get; set; }
    }
}
