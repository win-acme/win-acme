using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesArgumentsProvider : BaseArgumentsProvider<PemFilesArguments>
    {
        public override string Name => "PEM files plugin";
        public override string Group => "Store";
        public override string Condition => "--store pemfiles";

        public override void Configure(FluentCommandLineParser<PemFilesArguments> parser)
        {
            parser.Setup(o => o.PemFilesPath)
                 .As("pemfilespath")
                 .WithDescription(".pem files are exported to this folder");
        }

        public override bool Active(PemFilesArguments current) => !string.IsNullOrEmpty(current.PemFilesPath);
    }
}
