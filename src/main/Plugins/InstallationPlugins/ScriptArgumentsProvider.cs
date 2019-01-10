using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class ScriptArgumentsProvider : BaseArgumentsProvider<ScriptArguments>
    {
        public override string Name => "Script installer";

        public override void Configure(FluentCommandLineParser<ScriptArguments> parser)
        {
            parser.Setup(o => o.Script)
                .As("script")
                .WithDescription("[--installation script] Path to script to run after retrieving the certificate.");
            parser.Setup(o => o.ScriptParameters)
                .As("scriptparameters")
                .WithDescription("[--installation script] Parameters for the script to run after retrieving the certificate.");
        }

        public override bool Validate(ILogService log, ScriptArguments current, MainArguments main)
        {
            var active =
                !string.IsNullOrEmpty(current.Script) || 
                !string.IsNullOrEmpty(current.ScriptParameters);
            if (main.Renew)
            {
                log.Error("Installation parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return !active;
            }
            else
            {
                return true;
            }
        }
    }
}
