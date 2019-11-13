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
            return !string.IsNullOrEmpty(current.RegEx)
                || !string.IsNullOrEmpty(current.Simple);
        }

        public override void Configure(FluentCommandLineParser<IISBindingsArguments> parser)
        {
            parser.Setup(o => o.RegEx)
                .As("regex")
                .WithDescription("Regular expression to select hosts where the binding should be found.");
            parser.Setup(o => o.Simple)
                .As("simple")
                .WithDescription("Search expression (containing * and ? for placeholder) to select hosts where the binding should be found.");
        }
    }
}
