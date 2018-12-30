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

        public override IISBindingOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISBindingOptions();
            var filterSet = _helper.GetBindings(false, false);
            var chosenTarget = inputService.ChooseFromList(
                "Choose binding",
                filterSet.Where(x => x.Hidden == false),
                x => Choice.Create(x, description: $"{x.Host} (SiteId {x.Id})"),
                true);
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.Id;
                ret.Host = chosenTarget.Host;
                ret.FriendlyNameSuggestion = chosenTarget.Host;
                return ret;
            }
            else
            {
                return null;
            }
        }

        public override IISBindingOptions Default(IOptionsService optionsService)
        {
            var ret = new IISBindingOptions();
            var hostName = optionsService.TryGetRequiredOption(nameof(optionsService.Options.ManualHost), optionsService.Options.ManualHost).ToLower();
            var rawSiteId = optionsService.Options.SiteId;
            var filterSet = _helper.GetBindings(false, false);
            if (long.TryParse(rawSiteId, out long siteId))
            {
                filterSet = filterSet.Where(x => x.Id == siteId).ToList();
            }
            var chosenTarget = filterSet.Where(x => x.Host == hostName).FirstOrDefault();
            if (chosenTarget != null)
            {
                ret.SiteId = chosenTarget.Id;
                ret.Host = chosenTarget.Host;
                ret.FriendlyNameSuggestion = chosenTarget.Host;
                return ret;
            }
            else
            {
                return null;
            }
        }
    }
}