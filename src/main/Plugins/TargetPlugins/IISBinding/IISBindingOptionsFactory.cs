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

        public IISBindingOptionsFactory(ILogService log, IIISClient iisClient, IISBindingHelper helper) : base(log)
        {
            _iisClient = iisClient;
            _helper = helper;
        }

        public override IISBindingOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISBindingOptions();
            var bindings = _helper.GetBindings(arguments.MainArguments.HideHttps).Where(x => !x.Hidden);
            if (!bindings.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            var chosenTarget = inputService.ChooseFromList(
                "Choose binding",
                bindings,
                x => Choice.Create(x, description: $"{x.HostUnicode} (SiteId {x.SiteId})"),
                true);
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

        public override IISBindingOptions Default(IArgumentsService arguments)
        {
            var ret = new IISBindingOptions();
            var args = arguments.GetArguments<IISBindingArguments>();
            var hostName = arguments.TryGetRequiredArgument(nameof(args.Host), args.Host).ToLower();
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