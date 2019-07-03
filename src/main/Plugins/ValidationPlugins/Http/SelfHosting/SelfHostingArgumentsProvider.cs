using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    class SelfHostingArgumentsProvider : BaseArgumentsProvider<SelfHostingArguments>
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
        }

        public override bool Active(SelfHostingArguments current)
        {
            return current.ValidationPort != null;
        }
    }
}
