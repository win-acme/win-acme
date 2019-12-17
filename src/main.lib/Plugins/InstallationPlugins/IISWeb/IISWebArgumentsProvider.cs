using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebArgumentsProvider : BaseArgumentsProvider<IISWebArguments>
    {
        public override string Name => "IIS Web plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iis";

        public override void Configure(FluentCommandLineParser<IISWebArguments> parser)
        {
            parser.Setup(o => o.InstallationSiteId)
                .As("installationsiteid")
                .WithDescription("Specify site to install new bindings to. Defaults to the target if that is an IIS site.");
            parser.Setup(o => o.SSLPort)
                .As("sslport")
                .WithDescription($"Port number to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingPort}.");
            parser.Setup(o => o.SSLIPAddress)
                .As("sslipaddress")
                .WithDescription($"IP address to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingIp}.");
        }
    }
}
