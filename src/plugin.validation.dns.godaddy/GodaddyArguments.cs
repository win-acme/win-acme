using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class GodaddyArguments : BaseArguments
    {
        public override string Name => "GoDaddy";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation godaddy";

        [CommandLine(Description = "GoDaddy API key.")]
        public string ApiKey { get; set; }

        [CommandLine(Description = "GoDaddy API secret.", Secret = true)]
        public string ApiSecret { get; set; }
    }
}