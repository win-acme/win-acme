using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class CloudflareArguments : BaseArguments
    {
        public override string Name => "Cloudflare";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation cloudflare";

        [CommandLine(Description = "API Token for Cloudflare.", Secret = true)]
        public string CloudflareApiToken { get; set; }
    }
}
