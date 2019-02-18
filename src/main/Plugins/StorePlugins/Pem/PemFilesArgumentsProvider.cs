using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    class PemFilesArgumentsProvider : BaseArgumentsProvider<PemFilesArguments>
    {
        public override string Name => "PEM files plugin";
        public override string Group => "Store";
        public override string Condition => "--store pem";

        public override void Configure(FluentCommandLineParser<PemFilesArguments> parser)
        {
            parser.Setup(o => o.PemPath)
                 .As("pempath")
                 .WithDescription(".pem files are exported to this folder");
        }

        public override bool Active(PemFilesArguments current)
        {
            return !string.IsNullOrEmpty(current.PemPath);
        }
    }
}
