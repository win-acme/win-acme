using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class Level27Arguments : BaseArguments
    {
        public override string Name => "Level27";
        public override string Group => "Validation";
        public override string Condition => "--validation level27";

        [CommandLine(Description = "API key to authenticate against the Level27 API.", Secret = true)]
        public string? Level27ApiKey { get; set; }

        [CommandLine(Description = "OPTIONAL: Level27 API endpoint. Defaults to https://api.level27.eu/v1.")]
        public string? Level27ApiUrl { get; set; }
    }
}
