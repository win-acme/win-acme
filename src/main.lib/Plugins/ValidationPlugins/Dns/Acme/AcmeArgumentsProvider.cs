using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class AcmeArgumentsProvider : BaseArgumentsProvider<AcmeArguments>
    {
        public override string Name => "AcmeDns";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation acme-dns";

        public override void Configure(FluentCommandLineParser<AcmeArguments> parser)
        {
            parser.Setup(o => o.AcmeDnsServer)
                .As("acmednsserver")
                .WithDescription("Root URI of the acme-dns service");
        }

        public override bool Active(AcmeArguments current)
        {
            return !string.IsNullOrEmpty(current.AcmeDnsServer);
        }
    }
}
