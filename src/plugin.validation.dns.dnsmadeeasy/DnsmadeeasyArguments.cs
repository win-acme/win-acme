using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class DnsMadeEasyArguments : BaseArguments
    {
        public override string Name => "DnsMadeEasy";
        public override string Group => "Validation";
        public override string Condition => "--validation dnsmadeeasy";

        [CommandLine(Description = "DnsMadeEasy API key.")]
        public string? ApiKey { get; set; }

        [CommandLine(Description = "DnsMadeEasy API secret.", Secret = true)]
        public string? ApiSecret { get; set; }
    }
}