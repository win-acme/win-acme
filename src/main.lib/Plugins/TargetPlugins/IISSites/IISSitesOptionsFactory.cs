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
        protected IISSiteHelper _siteHelper;
        protected IISSiteOptionsHelper _optionsHelper;
        private ILogService _log;
        private IArgumentsService _arguments;

        public IISSitesOptionsFactory(ILogService log, IIISClient iisClient, 
            IISSiteHelper helper, IArgumentsService arguments)
        {
            _iisClient = iisClient;
            _siteHelper = helper;
            _log = log;
            _arguments = arguments;
            _optionsHelper = new IISSiteOptionsHelper(log);
        }

        public override IISSitesOptions Aquire(IInputService input, RunLevel runLevel)
        {
            var ret = new IISSitesOptions();
            var sites = _siteHelper.GetSites(_arguments.MainArguments.HideHttps, true).
                Where(x => x.Hidden == false).
                Where(x => x.Hosts.Any()).
                ToList();
            if (!sites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            input.WritePagedList(sites.Select(x => Choice.Create(x, $"{x.Name} ({x.Hosts.Count()} bindings)", x.Id.ToString())).ToList());
            var sanInput = input.RequestString("Enter a comma separated list of SiteIds or 'S' for all sites");
            sites = ProcessSiteIds(ret, sites, sanInput);
            if (sites == null)
            {
                return null;
            }
            var hosts = sites.SelectMany(x => x.Hosts).Distinct().OrderBy(x => x);
            if (_optionsHelper.AquireAdvancedOptions(input, hosts, runLevel, ret))
            {
                return ret;
            }
            return ret;
        }

        public override IISSitesOptions Default()
        {
            var ret = new IISSitesOptions();
            var args = _arguments.GetArguments<IISSiteArguments>();
            var sites = _siteHelper.GetSites(false, false);
            var rawSiteIds = _arguments.TryGetRequiredArgument(nameof(args.SiteId), args.SiteId);
            sites = ProcessSiteIds(ret, sites, rawSiteIds);
            if (sites == null)
            {
                return null;
            }
            if (_optionsHelper.DefaultAdvancedOptions(args, sites.SelectMany(s => s.Hosts), RunLevel.Unattended, ret))
            {
                return ret;
            }
            return null;
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
