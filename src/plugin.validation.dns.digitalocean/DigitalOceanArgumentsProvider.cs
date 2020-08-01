using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class DigitalOceanArgumentsProvider : BaseArgumentsProvider<DigitalOceanArguments>
    {
        public override string Name => "DigitalOcean";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation digitalocean";
        public override void Configure(FluentCommandLineParser<DigitalOceanArguments> parser)
        {
            _ = parser.Setup(o => o.ApiToken)
                .As("digitaloceanapitoken")
                .WithDescription("The API token to authenticate against the DigitalOcean API");
        }
    }
}