using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class DreamhostArguments: BaseArguments
    {
        public override string Name => "Dreamhost";
        public override string Group => "Validation";
        public override string Condition => "--validation dreamhost";

        [CommandLine(Description = "Dreamhost API key.", Secret = true)]
        public string? ApiKey { get; set; }
    }
}