using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PfxFileArguments : BaseArguments
    {
        public override string Name => "PFX file plugin";
        public override string Group => "Store";
        public override string Condition => "--store pfxfile";

        [CommandLine(Description = "Path to write the .pfx file to.")]
        public string? PfxFilePath { get; set; }

        [CommandLine(Description = "Password to set for .pfx files exported to the folder.", Secret = true)]
        public string? PfxPassword { get; set; }
    }
}
