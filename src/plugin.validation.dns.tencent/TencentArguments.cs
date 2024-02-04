using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class TencentArguments : BaseArguments
    {
        public override string Name => "Tencent";

        public override string Group => "Validation";

        public override string Condition => "--validation Tencent";

        [CommandLine(Description = "API ID for Tencent.", Secret = true)]
        public string? TencentApiID { get; set; }

        [CommandLine(Description = "API Key for Tencent.", Secret = true)]
        public string? TencentApiKey { get; set; }
    }
}
