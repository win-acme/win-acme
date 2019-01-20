using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISWebArgumentsProvider : BaseArgumentsProvider<IISWebArguments>
    {
        public override string Name => "IIS Web";
        public override string Group => "Installation";
        public override string Condition => "--installation iis (default)";

        public override void Configure(FluentCommandLineParser<IISWebArguments> parser)
        {
            parser.Setup(o => o.InstallationSiteId)
                .As("installationsiteid")
                .WithDescription("Specify site to install new bindings to. Defaults to the target site.");
            parser.Setup(o => o.SSLPort)
                .As("sslport")
                .SetDefault(IISClient.DefaultBindingPort)
                .WithDescription("Port to use for creating new HTTPS bindings.");
            parser.Setup(o => o.SSLIPAddress)
                .As("sslipaddress")
                .SetDefault(IISClient.DefaultBindingIp)
                .WithDescription("IP address to use for creating new HTTPS bindings.");
        }

        public override bool Active(IISWebArguments current)
        {
            return current.SSLIPAddress != IISClient.DefaultBindingIp ||
                current.SSLPort != IISClient.DefaultBindingPort ||
                current.InstallationSiteId != null;
        }

    }
}
