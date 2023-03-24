using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins;

public class InfomaniakArguments : BaseArguments
{
    public override string Name => "Infomaniak";
    public override string Group => "Validation";
    public override string Condition => "--validation infomaniak";

    [CommandLine(Description = "Infomaniak API token", Secret = true)]
    public string? ApiToken { get; set; }
}