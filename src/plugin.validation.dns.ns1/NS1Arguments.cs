using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class NS1Arguments : BaseArguments
    {
        public override string Name => "NS1";
        public override string Group => "Validation";
        public override string Condition => "--validation ns1";

        [CommandLine(Description = "NS1 API key.")]
        public string? ApiKey { get; set; }
    }
}
