using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISWebArgumentsProvider : BaseArgumentsProvider<IISWebArguments>
    {
        public override string Name => "IIS Web plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iis";
        public override bool Default => true;

        public override void Configure(FluentCommandLineParser<IISWebArguments> parser)
        {
            parser.Setup(o => o.InstallationSiteId)
                .As("installationsiteid")
                .WithDescription("Specify site to install new bindings to. Defaults to the target if that is an IIS site.");
            parser.Setup(o => o.SSLPort)
                .As("sslport")
                .SetDefault(IISClient.DefaultBindingPort)
                .WithDescription($"Port number to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingPort}.");
            parser.Setup(o => o.SSLIPAddress)
                .As("sslipaddress")
                .SetDefault(IISClient.DefaultBindingIp)
                .WithDescription($"IP address to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingIp}.");
        }

        public override bool Active(IISWebArguments current)
        {
            return current.SSLIPAddress != IISClient.DefaultBindingIp ||
                current.SSLPort != IISClient.DefaultBindingPort ||
                current.InstallationSiteId != null;
        }

    }
}
