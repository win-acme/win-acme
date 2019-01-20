using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class ScriptArgumentsProvider : BaseArgumentsProvider<ScriptArguments>
    {
        public override string Name => "Script";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation script";

        public override void Configure(FluentCommandLineParser<ScriptArguments> parser)
        {
            parser.Setup(o => o.DnsCreateScript)
                .As("dnscreatescript")
                .WithDescription("Path to script to create TXT record. Parameters passed are \"create\", host name, record name and desired content.");
            parser.Setup(o => o.DnsDeleteScript)
                .As("dnsdeletescript")
                .WithDescription("Path to script to remove TXT record. Parameters passed are \"delete\" the host name and record name.");
        }

        public override bool Active(ScriptArguments current)
        {
            return !string.IsNullOrEmpty(current.DnsCreateScript) ||
                !string.IsNullOrEmpty(current.DnsDeleteScript);
        }
    }
}
