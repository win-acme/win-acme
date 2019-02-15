using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    class ApacheArgumentsProvider : BaseArgumentsProvider<ApacheArguments>
    {
        public override string Name => "Apache plugin";
        public override string Group => "Store";
        public override string Condition => "--store apache";

        public override void Configure(FluentCommandLineParser<ApacheArguments> parser)
        {
            parser.Setup(o => o.ApacheCertificatePath)
                 .As("apachecertificatepath")
                 .WithDescription(".pem files are exported to this folder");
        }

        public override bool Active(ApacheArguments current)
        {
            return !string.IsNullOrEmpty(current.ApacheCertificatePath);
        }
    }
}
