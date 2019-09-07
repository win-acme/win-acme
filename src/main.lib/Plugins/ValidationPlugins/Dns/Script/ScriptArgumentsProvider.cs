using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptArgumentsProvider : BaseArgumentsProvider<ScriptArguments>
    {
        public override string Name => "Script";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation dnsscript";

        public override void Configure(FluentCommandLineParser<ScriptArguments> parser)
        {
            parser.Setup(o => o.DnsScript)
                .As("dnsscript")
                .WithDescription("Path to script that creates and deletes validation records, depending on its parameters. If this parameter is provided then --dnscreatescript and --dnsdeletescript are ignored.");
            parser.Setup(o => o.DnsCreateScript)
                .As("dnscreatescript")
                .WithDescription("Path to script that creates the validation TXT record.");
            parser.Setup(o => o.DnsCreateScriptArguments)
                .As("dnscreatescriptarguments")
                .WithDescription($"Default parameters passed to the script are {Script.DefaultCreateArguments}, but that can be customized using this argument.");
            parser.Setup(o => o.DnsDeleteScript)
                .As("dnsdeletescript")
                .WithDescription("Path to script to remove TXT record.");
            parser.Setup(o => o.DnsDeleteScriptArguments)
               .As("dnsdeletescriptarguments")
               .WithDescription($"Default parameters passed to the script are {Script.DefaultDeleteArguments}, but that can be customized using this argument.");
        }

        public override bool Active(ScriptArguments current)
        {
            return !string.IsNullOrEmpty(current.DnsScript) ||
                !string.IsNullOrEmpty(current.DnsCreateScript) ||
                !string.IsNullOrEmpty(current.DnsDeleteScript) ||
                !string.IsNullOrEmpty(current.DnsDeleteScriptArguments) ||
                !string.IsNullOrEmpty(current.DnsCreateScriptArguments);
        }
    }
}
