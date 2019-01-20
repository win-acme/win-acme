using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    class FileSystemArgumentsProvider : BaseArgumentsProvider<FileSystemArguments>
    {
        public override string Name => "FileSystem plugin";
        public override string Condition => "--validation filesystem";
        public override string Group => "Validation";

        public override void Configure(FluentCommandLineParser<FileSystemArguments> parser)
        {
            parser.Setup(o => o.ValidationSiteId)
                .As("validationsiteid")
                .WithDescription("Specify IIS site to use for handling validation requests. This will be used to choose the web root path.");
        }

        public override bool Active(FileSystemArguments current)
        {
            return current.ValidationSiteId != null;
        }
    }
}
