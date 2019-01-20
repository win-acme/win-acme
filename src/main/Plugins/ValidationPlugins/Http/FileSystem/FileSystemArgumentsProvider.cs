using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    class FileSystemArgumentsProvider : BaseArgumentsProvider<FileSystemArguments>
    {
        public override string Name => "FileSystem";
        public override string Condition => "--validationmode http-01 --validation filesystem";
        public override string Group => "Validation";

        public override void Configure(FluentCommandLineParser<FileSystemArguments> parser)
        {
            parser.Setup(o => o.ValidationSiteId)
                .As("validationsiteid")
                .WithDescription("Specify site to use for handling validation requests.");
        }

        public override bool Active(FileSystemArguments current)
        {
            return current.ValidationSiteId != null;
        }
    }
}
