//using PKISharp.WACS.Clients.IIS;
//using PKISharp.WACS.Plugins.Base.Factories;
//using PKISharp.WACS.Services;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PKISharp.WACS.Plugins.TargetPlugins
//{
//    internal class IISBindingOptionsFactory : TargetPluginOptionsFactory<IISBinding, IISBindingOptions>
//    {
//        public override async Task<IISBindingOptions> Aquire(IInputService inputService, RunLevel runLevel)
//        {
//            var ret = new IISBindingOptions();
//            var bindings = _helper.GetBindings().Where(x => !_arguments.MainArguments.HideHttps || x.Https == false);
//            if (!bindings.Any())
//            {
//                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
//                return null;
//            }
//            var chosenTarget = await inputService.ChooseFromList(
//                "Choose binding",
//                bindings,
//                x => Choice.Create(x, color: x.Https ? ConsoleColor.Gray : (ConsoleColor?)null),
//                "Abort");
//            if (chosenTarget != null)
//            {
//                ret.SiteId = chosenTarget.SiteId;
//                ret.Host = chosenTarget.HostUnicode;
//                return ret;
//            }
//            return null;
//        }
//    }
//}