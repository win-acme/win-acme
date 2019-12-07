using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingsArgumentsProvider : BaseArgumentsProvider<IISBindingsArguments>
    {
        public override string Name => "IIS plugin";
        public override string Group => "Target";
        public override string Condition => "--target iis";

        public override void Configure(FluentCommandLineParser<IISBindingsArguments> parser)
        {
            parser.Setup(o => o.SiteId)
                .As("siteid")
                .WithDescription("Identifiers of one or more sites to include. " +
                "This may be a comma seperated list.");
            parser.Setup(o => o.Host)
                .As("host")
                .WithDescription("Host name to filter. This parameter may be used to target specific bindings. " +
                "This may be a comma seperated list.");
            parser.Setup(o => o.Pattern)
                .As("hosts-pattern")
                .WithDescription("Pattern filter for host names. Can be used to dynamically include bindings " +
                "based on their match with the pattern. You may use a `*` for a range of any characters and a `?` " +
                "for any single character. For example: the pattern `example.*` will match `example.net` and " +
                "`example.com` (but not `my.example.com`) and the pattern `?.example.com` will match " +
                "`a.example.com` and `b.example.com` (but not `www.example.com`).");
            parser.Setup(o => o.Regex)
                .As("hosts-regex")
                .WithDescription("Regex pattern filter for host names. Some people, when confronted with a " +
                "problem, think \"I know, I'll use regular expressions.\" Now they have two problems.");
            parser.Setup(o => o.CommonName)
                .As("commonname")
                .WithDescription("Specify the common name of the certificate that should be requested for the target. By default this will be the first binding that is enumerated.");
            parser.Setup(o => o.ExcludeBindings)
                .As("excludebindings")
                .WithDescription("Exclude host names from the certificate. This may be a comma separated list.");
        }
    }
}
