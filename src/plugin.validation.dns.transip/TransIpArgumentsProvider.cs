using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public sealed class TransIpArgumentsProvider : BaseArgumentsProvider<TransIpArguments>
    {
        public override string Name { get; } = "TransIp";
        public override string Group { get; } = "Validation";
        public override string Condition { get; } = "--validationmode dns-01 --validation transip";
        public override void Configure(FluentCommandLineParser<TransIpArguments> parser)
        {
            _ = parser.Setup(_ => _.Login)
              .As("transip-login")
              .WithDescription("Login name at TransIp.");

            _ = parser.Setup(_ => _.PrivateKey)
              .As("transip-privatekey")
              .WithDescription("Private key generated in the control panel.");
        }
    }
}