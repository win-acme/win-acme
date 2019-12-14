using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class CsrArgumentsProvider : BaseArgumentsProvider<CsrArguments>
    {
        public override string Name => "Common";
        public override string Group => "CSR";
        public override string? Condition => null;

        public override void Configure(FluentCommandLineParser<CsrArguments> parser)
        {
            parser.Setup(o => o.OcspMustStaple)
                .As("ocsp-must-staple")
                .WithDescription("Enable OCSP Must Staple extension on certificate.");
            parser.Setup(o => o.ReusePrivateKey)
                .As("reuse-privatekey")
                .WithDescription("Reuse the same private key for each renewal.");
        }
    }
}
