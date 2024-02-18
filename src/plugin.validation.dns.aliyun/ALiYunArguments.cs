using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class ALiYunArguments : BaseArguments
    {
        public override string Name => "ALiYun";

        public override string Group => "Validation";

        public override string Condition => "--validation aliyun";

        [CommandLine(Description = "DNS Server Domain Name\r\nRefer: https://api.aliyun.com/product/Alidns", Secret = false)]
        public string? ALiYunServer { get; set; } = "dns.aliyuncs.com";

        [CommandLine(Description = "API ID for ALiYun.", Secret = true)]
        public string? ALiYunApiID { get; set; }

        [CommandLine(Description = "API Secret for ALiYun.", Secret = true)]
        public string? ALiYunApiSecret { get; set; }
    }
}
