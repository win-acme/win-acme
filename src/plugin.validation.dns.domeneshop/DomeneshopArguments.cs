
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public sealed class DomeneshopArguments : BaseArguments
    {
        public override string Name { get; } = "Domeneshop";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validation domeneshop";

        [CommandLine(Description = "Domeneshop ClientID (token)")]
        public string? ClientId { get; set; }

        [CommandLine(Description = "Domeneshop Client Secret")]
        public string? ClientSecret { get; set; }
    }
}
