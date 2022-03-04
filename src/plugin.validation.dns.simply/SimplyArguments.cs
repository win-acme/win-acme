using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class SimplyArguments : BaseArguments
    {
        public override string Name => "Simply";
        public override string Group => "Validation";
        public override string Condition => "--validation simply";

        [CommandLine(Description = "Simply Account.")]
        public string? Account { get; set; }

        [CommandLine(Description = "Simply API key.", Secret = true)]
        public string? ApiKey { get; set; }
    }
}