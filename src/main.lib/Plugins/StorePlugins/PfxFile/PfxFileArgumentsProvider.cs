using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PfxFileArgumentsProvider : BaseArgumentsProvider<PfxFileArguments>
    {
        public override string Name => "PFX file plugin";
        public override string Group => "Store";
        public override string Condition => "--store pfxfile";

        public override void Configure(FluentCommandLineParser<PfxFileArguments> parser)
        {
            parser.Setup(o => o.PfxFilePath)
                 .As("pfxfilepath")
                 .WithDescription("Path to write the .pfx file to.");
            parser.Setup(o => o.PfxPassword)
                .As("pfxpassword")
                .WithDescription("Password to set for .pfx files exported to the IIS CSS.");
        }
    }
}
