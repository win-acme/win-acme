using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class UserArguments : BaseArguments
    {
        public override string Name => "User Store plugin";
        public override string Group => "Store";
        public override string Condition => "--store userstore";

        [CommandLine(Description = "While renewing, do not remove the previous certificate.")]
        public bool KeepExisting { get; set; }
    }
}
