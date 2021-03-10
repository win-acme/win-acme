using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingArgumentsProvider : BaseArgumentsProvider<SelfHostingArguments>
    {
        public override string Name => "SelfHosting plugin";
        public override string Group => "Validation";
        public override string Condition => "--validation selfhosting";
        public override bool Default => true;

        public override void Configure(FluentCommandLineParser<SelfHostingArguments> parser)
        {
            parser.Setup(o => o.ValidationPort)
                .As("validationport")
                .WithDescription("Port to use for listening to validation requests. Note that the ACME server will always send requests to port 80. This option is only useful in combination with a port forwarding.");
            parser.Setup(o => o.ValidationProtocol)
              .As("validationprotocol")
              .WithDescription("Protocol to use to handle validation requests. Defaults to http but may be set to https if you have automatic redirects setup in your infrastructure before requests hit the web server.");
        }
    }
}
