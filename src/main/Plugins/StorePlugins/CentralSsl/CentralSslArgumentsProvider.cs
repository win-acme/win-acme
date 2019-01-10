using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    class CentralSslArgumentsProvider : BaseArgumentsProvider<CentralSslArguments>
    {
        public override string Name => "CentralSsl";
        public override string Group => "Store";
        public override string Condition => "--store centralsslstore (default)";

        public override void Configure(FluentCommandLineParser<CentralSslArguments> parser)
        {
            parser.Setup(o => o.CentralSslStore)
                 .As("centralsslstore")
                 .WithDescription("When using this setting, certificate files are stored to the CCS and IIS bindings are configured to reflect that.");
            parser.Setup(o => o.PfxPassword)
                .As("pfxpassword")
                .WithDescription("Password to set for .pfx files exported to the IIS CSS.");
        }

        public override bool Validate(ILogService log, CentralSslArguments current, MainArguments main)
        {
            var active =
                !string.IsNullOrEmpty(current.CentralSslStore) ||
                !string.IsNullOrEmpty(current.PfxPassword);
            if (main.Renew && active)
            {
                log.Error("Store parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
