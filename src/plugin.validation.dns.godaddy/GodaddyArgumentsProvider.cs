using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class GodaddyArgumentsProvider : BaseArgumentsProvider<GodaddyArguments>
    {
        public override string Name => "Godaddy";

        public override string Group => "Validation";

        public override string Condition => "--validationmode dns-01 --validation godday";

        public override void Configure(FluentCommandLineParser<GodaddyArguments> parser)
        {
            _ = parser.Setup(o => o.ApiKey)
                .As("apikey")
                .WithDescription("GoDaddy API key.");
        }
    }
}
