using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingsArgumentsProvider : BaseArgumentsProvider<IISBindingsArguments>
    {
        public override string Name => "IIS Bindings plugin";
        public override string Group => "Target";
        public override string Condition => "--target iisbindings";

        public override bool Active(IISBindingsArguments current)
        {
            return !string.IsNullOrEmpty(current.Regex)
                || !string.IsNullOrEmpty(current.Pattern)
                || !string.IsNullOrEmpty(current.Host);
        }

        public override void Configure(FluentCommandLineParser<IISBindingsArguments> parser)
        {
            parser.Setup(o => o.SiteId)
                .As("siteid")
                .WithDescription("Id of the site where the binding should be found (optional).");
            parser.Setup(o => o.Host)
                .As("host")
                .WithDescription("Host of the binding to get a certificate for. This may be a comma separated list.");
            parser.Setup(o => o.Regex)
                .As("regex")
                .WithDescription("Regular expression to select hosts where the binding should be found.");
            parser.Setup(o => o.Pattern)
                .As("pattern")
                .WithDescription("Search expression (containing * and ? for placeholder) to select hosts where the binding should be found.");
        }
    }
}
