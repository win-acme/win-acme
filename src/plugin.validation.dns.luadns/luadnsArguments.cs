using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class LuaDnsArguments : BaseArguments
    {
        public override string Name { get; } = "LuaDns";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validationmode dns-01 --validation luadns";

        [CommandLine(Description = "LuaDNS account username (email address).")]
        public string LuaDnsUsername { get; set; }

        [CommandLine(Description = "LuaDNS API key.", Secret = true)]
        public string LuaDnsAPIKey { get; set; }
    }
}