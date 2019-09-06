using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingOptionsFactory : TargetPluginOptionsFactory<IISBinding, IISBindingOptions>
    {
        public override bool Hidden => !_iisClient.HasWebSites;
        protected IIISClient _iisClient;
        protected IISBindingHelper _helper;
        private ILogService _log;
        private IArgumentsService _arguments;

        public IISBindingOptionsFactory(
            ILogService log, IIISClient iisClient, 
            IISBindingHelper helper, IArgumentsService arguments)
        {
            _iisClient = iisClient;
            _helper = helper;
            _log = log;
            _arguments = arguments;
        }

        public override IISBindingOptions Aquire(IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISBindingOptions();
            var bindings = _helper.GetBindings(_arguments.MainArguments.HideHttps).Where(x => !x.Hidden);
            if (!bindings.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            var chosenTarget = inputService.ChooseFromList(
                "Choose binding",
                bindings,
                x => Choice.Create(x),
                "Abort");
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.SiteId;
                ret.Host = chosenTarget.HostUnicode;
                return ret;
            }
            else
            {
                return null;
            }
        }

        public override IISBindingOptions Default()
        {
            var ret = new IISBindingOptions();
            var args = _arguments.GetArguments<IISBindingArguments>();
            var hostName = _arguments.TryGetRequiredArgument(nameof(args.Host), args.Host).ToLower();
            var rawSiteId = args.SiteId;
            var filterSet = _helper.GetBindings(false);
            if (!string.IsNullOrEmpty(rawSiteId))
            {
                if (long.TryParse(rawSiteId, out long siteId))
                {
                    filterSet = filterSet.Where(x => x.SiteId == siteId).ToList();
                }
                else
                {
                    _log.Error("Invalid SiteId {siteId}", rawSiteId);
                    return null;
                }
            }
            var chosenTarget = filterSet.Where(x => x.HostUnicode == hostName || x.HostPunycode == hostName).FirstOrDefault();
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.SiteId;
                ret.Host = chosenTarget.HostUnicode;
                return ret;
            }
            else
            {
                return null;
            }
        }
    }
}