using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualArgumentsProvider : BaseArgumentsProvider<ManualArguments>
    {
        public override string Name => "Manual plugin";
        public override string Group => "Target";
        public override string Condition => "--target manual";

        public override void Configure(FluentCommandLineParser<ManualArguments> parser)
        {
            parser.Setup(o => o.CommonName)
                .As("commonname")
                .WithDescription("Specify the common name of the certificate. If not provided the first host name will be used.");
            parser.Setup(o => o.Host)
                .As("host")
                .WithDescription("A host name to get a certificate for. This may be a comma-separated list.");
        }
    }
}
