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
//        private readonly IISBindingHelper _helper;
//        private readonly ILogService _log;
//        private readonly IArgumentsService _arguments;

//        public IISBindingOptionsFactory(
//            ILogService log, IIISClient iisClient,
//            IISBindingHelper helper, IArgumentsService arguments,
//            UserRoleService userRoleService)
//        {
//            _helper = helper;
//            _log = log;
//            _arguments = arguments;
//            Hidden = !(iisClient.Version.Major > 6);
//            Disabled = IISBinding.Disabled(userRoleService);
//        }

//        public override int Order => 1;

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

//        public override Task<IISBindingOptions> Default()
//        {
//            var ret = new IISBindingOptions();
//            var args = _arguments.GetArguments<IISBindingArguments>();
//            var hostName = _arguments.TryGetRequiredArgument(nameof(args.Host), args.Host).ToLower();
//            var rawSiteId = args.SiteId;
//            var filterSet = _helper.GetBindings();
//            if (!string.IsNullOrEmpty(rawSiteId))
//            {
//                if (long.TryParse(rawSiteId, out var siteId))
//                {
//                    filterSet = filterSet.Where(x => x.SiteId == siteId).ToList();
//                }
//                else
//                {
//                    _log.Error("Invalid SiteId {siteId}", rawSiteId);
//                    return Task.FromResult(default(IISBindingOptions));
//                }
//            }
//            var chosenTarget = filterSet.Where(x => x.HostUnicode == hostName || x.HostPunycode == hostName).FirstOrDefault();
//            if (chosenTarget != null)
//            {
//                ret.SiteId = chosenTarget.SiteId;
//                ret.Host = chosenTarget.HostUnicode;
//                return Task.FromResult(ret);
//            }
//            else
//            {
//                return Task.FromResult(default(IISBindingOptions));
//            }
//        }
//    }
//}