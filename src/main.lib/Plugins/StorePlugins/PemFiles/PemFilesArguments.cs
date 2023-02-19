using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesArguments : BaseArguments
    {
        public override string Name => "PEM files plugin";
        public override string Group => "Store";
        public override string Condition => "--store pemfiles";

        [CommandLine(Description = ".pem files are exported to this folder.")]
        public string? PemFilesPath { get; set; }

        [CommandLine(Description = "Prefix to use for the .pem files, defaults to the common name.")]
        public string? PemFilesName { get; set; }

        [CommandLine(Description = "Password to set for the private key .pem file.", Secret = true)]
        public string? PemPassword { get; set; }
    }
}
