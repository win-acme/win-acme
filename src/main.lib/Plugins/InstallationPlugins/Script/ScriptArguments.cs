using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptArguments : BaseArguments
    {
        public override string Name => "Script plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation script";

        [CommandLine(Description = "Path to script file to run after retrieving the certificate. This may be any executable file or a Powershell (.ps1) script.")]
        public string? Script { get; set; }

        [CommandLine(Description = "Parameters for the script to run after retrieving the certificate. Refer to https://win-acme.com/reference/plugins/installation/script for further instructions.")]
        public string? ScriptParameters { get; set; }
    }
}
