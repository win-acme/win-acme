using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslArguments : BaseArguments
    {
        public override string Name => "Central Certificate Store plugin";
        public override string Group => "Store";
        public override string Condition => "--store centralssl";

        [CommandLine(Description = "Location of the IIS Central Certificate Store.")]
        public string? CentralSslStore { get; set; }

        [CommandLine(Description = "Password to set for .pfx files exported to the IIS Central Certificate Store.", Secret = true)]
        public string? PfxPassword { get; set; }
    }
}
