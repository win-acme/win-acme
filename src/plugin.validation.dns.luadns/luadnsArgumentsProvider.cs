using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class LuaDnsArgumentsProvider : BaseArgumentsProvider<LuaDnsArguments>
    {
        public override string Name { get; } = "LuaDns";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validationmode dns-01 --validation LuaDns";
        public override void Configure(FluentCommandLineParser<LuaDnsArguments> parser)
        {
            _ = parser.Setup(_ => _.LuaDnsUsername)
                .As("LuaDnsUsername")
                .WithDescription("LuaDNS account username (email address)");

            _ = parser.Setup(_ => _.LuaDnsAPIKey)
                .As("LuaDnsAPIKey")
                .WithDescription("LuaDNS API key");
        }
    }
}