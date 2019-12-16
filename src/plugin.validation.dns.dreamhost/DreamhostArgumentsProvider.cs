using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class DreamhostArgumentsProvider : BaseArgumentsProvider<DreamhostArguments>
    {
        public override string Name => "Dreamhost";

        public override string Group => "Validation";

        public override string Condition => "--validationmode dns-01 --validation dreamhost";

        public override void Configure(FluentCommandLineParser<DreamhostArguments> parser)
        {
            parser.Setup(o => o.ApiKey)
                .As("apiKey")
                .WithDescription("Dreamhost API key.");
        }
    }
}
