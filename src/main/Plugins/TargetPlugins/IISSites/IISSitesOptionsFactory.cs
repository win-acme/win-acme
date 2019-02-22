using PKISharp.WACS.Clients.IIS;
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
        protected IIISClient _iisClient;
        protected IISSiteHelper _helper;

        public IISSitesOptionsFactory(ILogService log, IIISClient iisClient, IISSiteHelper helper) : base(log)
        {
            _iisClient = iisClient;
            _helper = helper;
        }

        public override IISSitesOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISSitesOptions();
            var sites = _helper.GetSites(arguments.MainArguments.HideHttps, true).
                Where(x => x.Hidden == false).
                Where(x => x.Hosts.Any()).
                ToList();
            if (!sites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            inputService.WritePagedList(sites.Select(x => Choice.Create(x, $"{x.Name} ({x.Hosts.Count()} bindings)", x.Id.ToString())).ToList());
            var sanInput = inputService.RequestString("Enter a comma separated list of SiteIds or 'S' for all sites").ToLower().Trim();
            sites = ProcessSiteIds(ret, sites, sanInput);
            if (sites == null)
            {
                return null;
            }
            var hosts = sites.SelectMany(x => x.Hosts).Distinct().OrderBy(x => x);
            inputService.WritePagedList(hosts.Select(x => Choice.Create(x, "")));
            ret.ExcludeBindings = inputService.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions").ParseCsv();
            var remaining = hosts.Except(ret.ExcludeBindings ?? new List<string>());
            if (remaining.Count() == 0)
            {
                _log.Error("No bindings remain after excluding");
                return null;
            }

            // Set common name
            if (remaining.Count() > 1)
            {
                ret.CommonName = inputService.ChooseFromList(
                    "Select primary domain (common name)",
                    remaining,
                    x => new Choice<string>(x),
                    true);
            }
            return ret;
        }

        public override IISSitesOptions Default(IArgumentsService arguments)
        {
            var ret = new IISSitesOptions();
            var args = arguments.GetArguments<IISSiteArguments>();
            var sites = _helper.GetSites(false, false);
            var rawSiteIds = arguments.TryGetRequiredArgument(nameof(args.SiteId), args.SiteId);
            sites = ProcessSiteIds(ret, sites, rawSiteIds);
            if (sites == null)
            {
                return null;
            }
            ret.ExcludeBindings = args.ExcludeBindings.ParseCsv();
            if (ret.ExcludeBindings != null)
            {
                ret.ExcludeBindings = ret.ExcludeBindings.Select(x => x.ConvertPunycode()).ToList();
            }
            var commonName = args.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = commonName.ToLower().Trim().ConvertPunycode();
                if (sites.Any(s => s.Hosts.Contains(commonName)) &&
                    (ret.ExcludeBindings == null || !ret.ExcludeBindings.Contains(commonName)))
                {
                    ret.CommonName = commonName;
                }
                else
                {
                    _log.Error("Common name {commonName} not found or excluded", commonName);
                    return null;
                }
            }
            return ret;
        }

        private List<IISSiteHelper.IISSiteOption> ProcessSiteIds(IISSitesOptions options, List<IISSiteHelper.IISSiteOption> sites, string sanInput)
        {
            if (string.Equals(sanInput, "s", StringComparison.InvariantCultureIgnoreCase))
            {
                options.All = true;
            }
            else
            {
                sites = FilterOptions(sites, sanInput);
                if (sites == null)
                {
                    return null;
                }
                options.SiteIds = sites.Select(x => x.Id).OrderBy(x => x).ToList();
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
                            _log.Error($"SiteId '{idString}' not found");
                            return null;
                        }
                    }
                    else
                    {
                        _log.Error($"Invalid SiteId '{idString}', should be a number");
                        return null;
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
