using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class LinodeArguments : BaseArguments
    {
        public override string Name => "Linode";
        public override string Group => "Validation";
        public override string Condition => "--validation linode";

        [CommandLine(Description = "Linode Personal Access Token", Secret = true)]
        public string? ApiToken { get; set; }
    }
}
