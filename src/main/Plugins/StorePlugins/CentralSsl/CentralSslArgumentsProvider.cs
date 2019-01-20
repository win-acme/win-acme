using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    class CentralSslArgumentsProvider : BaseArgumentsProvider<CentralSslArguments>
    {
        public override string Name => "CCS plugin";
        public override string Group => "Store";
        public override string Condition => "--store centralsslstore";

        public override void Configure(FluentCommandLineParser<CentralSslArguments> parser)
        {
            parser.Setup(o => o.CentralSslStore)
                 .As("centralsslstore")
                 .WithDescription("When using this setting, certificate files are stored to the CCS and IIS bindings are configured to reflect that.");
            parser.Setup(o => o.PfxPassword)
                .As("pfxpassword")
                .WithDescription("Password to set for .pfx files exported to the IIS CSS.");
        }

        public override bool Active(CentralSslArguments current)
        {
            return !string.IsNullOrEmpty(current.CentralSslStore) ||
                !string.IsNullOrEmpty(current.PfxPassword);
        }

    }
}
