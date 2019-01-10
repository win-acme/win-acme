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
                .WithDescription("Path to script to create TXT record. Parameters passed are the host name, record name and desired content.");
            parser.Setup(o => o.DnsDeleteScript)
                .As("dnsdeletescript")
                .WithDescription("Path to script to remove TXT record. Parameters passed are the host name and record name.");
        }

        public override bool Validate(ILogService log, ScriptArguments current, MainArguments main)
        {
            var active =
                !string.IsNullOrEmpty(current.DnsCreateScript) ||
                !string.IsNullOrEmpty(current.DnsDeleteScript);
            if (main.Renew)
            {
                log.Error("Validation parameters cannot be changed during a renewal. Recreate/overwrite the renewal or edit the .json file if you want to make changes.");
                return !active;
            }
            else
            {
                return true;
            }
        }
    }
}
