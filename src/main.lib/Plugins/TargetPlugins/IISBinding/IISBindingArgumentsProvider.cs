//using Fclp;
//using PKISharp.WACS.Configuration;

//namespace PKISharp.WACS.Plugins.TargetPlugins
//{
//    internal class IISBindingArgumentsProvider : BaseArgumentsProvider<IISBindingArguments>
//    {
//        public override string Name => "IIS Binding plugin";
//        public override string Group => "Target";
//        public override string Condition => "--target iisbinding";

//        public override bool Active(IISBindingArguments current)
//        {
//            return !string.IsNullOrEmpty(current.SiteId) ||
//                !string.IsNullOrEmpty(current.Host);
//        }

//        public override void Configure(FluentCommandLineParser<IISBindingArguments> parser)
//        {
//            parser.Setup(o => o.SiteId)
//                .As("siteid")
//                .WithDescription("Id of the site where the binding should be found (optional).");
//            parser.Setup(o => o.Host)
//                .As("host")
//                .WithDescription("Host of the binding to get a certificate for.");
//        }
//    }
//}
