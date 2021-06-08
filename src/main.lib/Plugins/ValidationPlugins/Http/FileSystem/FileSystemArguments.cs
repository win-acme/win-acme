using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FileSystemArguments : HttpValidationArguments
    {
        public override string Name => "FileSystem plugin";
        public override string Condition => "--validation filesystem";
        public override string Group => "Validation";

        [CommandLine(Description = "Specify IIS site to use for handling validation requests. This will be used to choose the web root path.")]
        public long? ValidationSiteId { get; set; }
    }
}
