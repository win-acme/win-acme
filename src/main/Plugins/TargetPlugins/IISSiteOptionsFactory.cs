using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSiteOptionsFactory : TargetPluginOptionsFactory<IISSite, IISSiteOptions>
    {
        public override bool Hidden => !_iisClient.HasWebSites;
        protected IIISClient _iisClient;
        protected IISSiteHelper _helper;
        public IISSiteOptionsFactory(ILogService log, IIISClient iisClient, IISSiteHelper helper) : base(log)
        {
            _iisClient = iisClient;
            _helper = helper;
        }

        public override IISSiteOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISSiteOptions();
            var chosen = inputService.ChooseFromList("Choose site",
                _helper.GetSites(optionsService.Options.HideHttps, true).Where(x => x.Hidden == false), 
                x => new Choice<IISSiteHelper.IISSiteOption>(x) { Description = x.Name },
                true);
            if (chosen != null)
            {
                ret.SiteId = chosen.Id;
                ret.FriendlyNameSuggestion = $"Site-{chosen.Id}";

                // Exclude bindings 
                inputService.WritePagedList(chosen.Hosts.Select(x => Choice.Create(x, "")));
                ret.ExcludeBindings = inputService.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions").ParseCsv();
                if (runLevel.HasFlag(RunLevel.Advanced))
                {
                    ret.CommonName = inputService.ChooseFromList(
                        "Select common name",
                        chosen.Hosts,
                        x => new Choice<string>(x), 
                        false);
                }
                return ret;
            }
            return null;
        }

        public override IISSiteOptions Default(IOptionsService optionsService)
        {
            var ret = new IISSiteOptions();
            var rawSiteId = optionsService.TryGetRequiredOption(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId);
            if (long.TryParse(rawSiteId, out long siteId))
            {
                var site = _helper.GetSites(false, false).FirstOrDefault(binding => binding.Id == siteId);
                if (site != null)
                {
                    ret.SiteId = site.Id;
                    ret.ExcludeBindings = optionsService.Options.ExcludeBindings.ParseCsv();
                    ret.FriendlyNameSuggestion = $"Site-{ret.SiteId}";
                    var commonName = optionsService.Options.CommonName.ToLower();
                    if (!string.IsNullOrEmpty(commonName))
                    {
                        if (site.Hosts.Contains(commonName))
                        {
                            ret.CommonName = commonName;
                        }
                        else
                        {
                            _log.Error("Specified common name {commonName} not found in site", commonName);
                            return null;
                        }
                    }
                }
                else
                {
                    _log.Error("Unable to find SiteId {siteId}", siteId);
                }
            }
            else
            {
                _log.Error("Invalid SiteId {siteId}", optionsService.Options.SiteId);
            }
            return null;
        }
    }
}
