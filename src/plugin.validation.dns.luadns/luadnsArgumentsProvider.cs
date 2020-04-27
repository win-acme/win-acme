using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class LUADNSArgumentsProvider : BaseArgumentsProvider<LUADNSArguments>
    {
        public override string Name { get; } = "LUADNS";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validationmode dns-01 --validation luadns";
        public override void Configure(FluentCommandLineParser<LUADNSArguments> parser)
        {
            parser.Setup(_ => _.LUADNSUsername)
                .As("LUADNSUsername")
                .WithDescription("LUADN account useername (email address)");

            parser.Setup(_ => _.LUADNSAPIKey)
                .As("LUADNSAPIKey")
                .WithDescription("LUADNS API Key");
        }
    }
}