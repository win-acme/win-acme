using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSitesOptionsFactory : TargetPluginOptionsFactory<IISSites, IISSitesOptions>
    {
        public override bool Hidden => !_iisClient.HasWebSites;
        protected IISClient _iisClient;
        protected IISSiteHelper _helper;

        public IISSitesOptionsFactory(ILogService log, IISClient iisClient, IISSiteHelper helper) : base(log)
        {
            _iisClient = iisClient;
            _helper = helper;
        }

        public override IISSitesOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISSitesOptions();
            var sites = _helper.GetSites(optionsService.Options.HideHttps, true).Where(x => x.Hidden == false).ToList();
            inputService.WritePagedList(sites.Select(x => Choice.Create(x, $"{x.Name} ({x.Hosts.Count()} bindings)", x.Id.ToString())).ToList());
            var sanInput = inputService.RequestString("Enter a comma separated list of site IDs, or 'S' to run for all sites").ToLower().Trim();
            sites = ProcessSiteIds(ret, sites, sanInput);

            var sanChoices = sites.SelectMany(x => x.Hosts).Distinct().OrderBy(x => x);
            inputService.WritePagedList(sanChoices.Select(x => Choice.Create(x, "")));
            ret.ExcludeBindings = inputService.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions").ParseCsv();

            if (runLevel >= RunLevel.Advanced)
            {
                ret.CommonName = inputService.ChooseFromList<string, string>(
                    "Choose a domain name to be the certificate's common name",
                    sanChoices,
                    (x) => new Choice<string>(x),
                    false);
            }
            return ret;
        }

        public override IISSitesOptions Default(IOptionsService optionsService)
        {
            var ret = new IISSitesOptions();
            var sites = _helper.GetSites(false, false);
            var rawSiteIds = optionsService.TryGetRequiredOption(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId);
            sites = ProcessSiteIds(ret, sites, rawSiteIds);
            var commonName = optionsService.Options.CommonName.ToLower();
            if (string.IsNullOrEmpty(commonName))
            {
                if (sites.SelectMany(x => x.Hosts).Contains(commonName))
                {
                    ret.CommonName = commonName;
                }
                else
                {
                    _log.Error("Specified common name {commonName} not found in sites", commonName);
                    return null;
                }
            }
            ret.ExcludeBindings = optionsService.Options.ExcludeBindings.ParseCsv();
            return ret;
        }

        private List<IISSiteHelper.IISSiteOption> ProcessSiteIds(IISSitesOptions options, List<IISSiteHelper.IISSiteOption> sites, string sanInput)
        {
            if (string.Equals(sanInput, "s", StringComparison.InvariantCultureIgnoreCase))
            {
                options.All = true;
                options.FriendlyNameSuggestion = "Sites-all";
            }
            else
            {
                sites = FilterOptions(sites, sanInput);
                options.SiteIds = sites.Select(x => x.Id).ToList();
                options.FriendlyNameSuggestion = $"Sites-{options.SiteIds}";
            }
            return sites;
        }

        private List<IISSiteHelper.IISSiteOption> FilterOptions(List<IISSiteHelper.IISSiteOption> targets, string sanInput)
        {
            var siteList = new List<IISSiteHelper.IISSiteOption>();
            if (string.Equals(sanInput, "s", StringComparison.InvariantCultureIgnoreCase))
            {
                return targets;
            }
            else
            {
                var siteIDs = sanInput.ParseCsv();
                foreach (var idString in siteIDs)
                {
                    if (int.TryParse(idString, out var id))
                    {
                        var site = targets.Where(t => t.Id == id).FirstOrDefault();
                        if (site != null)
                        {
                            siteList.Add(site);
                        }
                        else
                        {
                            _log.Warning($"SiteId '{idString}' not found");
                        }
                    }
                    else
                    {
                        _log.Warning($"Invalid SiteId '{idString}', should be a number");
                    }
                }
                if (siteList.Count == 0)
                {
                    _log.Warning($"No valid sites selected");
                    return null;
                }
            }
            return siteList;
        }
    }
}
