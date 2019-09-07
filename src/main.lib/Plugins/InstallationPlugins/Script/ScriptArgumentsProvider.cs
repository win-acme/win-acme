using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptArgumentsProvider : BaseArgumentsProvider<ScriptArguments>
    {
        public override string Name => "Script plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation script";

        public override void Configure(FluentCommandLineParser<ScriptArguments> parser)
        {
            parser.Setup(o => o.Script)
                .As("script")
                .WithDescription("Path to script file to run after retrieving the certificate. This may be a .exe or .bat. Refer to the Wiki for instructions on how to run .ps1 files.");
            parser.Setup(o => o.ScriptParameters)
                .As("scriptparameters")
                .WithDescription("Parameters for the script to run after retrieving the certificate. Refer to the Wiki for further instructions.");
        }

        public override bool Active(ScriptArguments current)
        {
            return !string.IsNullOrEmpty(current.Script) ||
                !string.IsNullOrEmpty(current.ScriptParameters);
        }
    }
}
