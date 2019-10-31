using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSitesOptionsFactory : TargetPluginOptionsFactory<IISSites, IISSitesOptions>
    {
        protected IIISClient _iisClient;
        protected IISSiteHelper _siteHelper;
        protected IISSiteOptionsHelper _optionsHelper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISSitesOptionsFactory(ILogService log, IIISClient iisClient,
            IISSiteHelper helper, IArgumentsService arguments,
            UserRoleService userRoleService)
        {
            _iisClient = iisClient;
            _siteHelper = helper;
            _log = log;
            _arguments = arguments;
            _optionsHelper = new IISSiteOptionsHelper(log);
            Hidden = !(iisClient.Version.Major > 6);
            Disabled = IISSites.Disabled(userRoleService);
        }

        public override int Order => 3;

        public override async Task<IISSitesOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var ret = new IISSitesOptions();
            var sites = _siteHelper.GetSites(true).
                Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).
                Where(x => x.Hosts.Any()).
                ToList();
            if (!sites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            input.WritePagedList(
                sites.Select(x => 
                    Choice.Create(x, 
                        $"{x.Name} ({x.Hosts.Count()} binding{(x.Hosts.Count()==1?"":"s")})", 
                        x.Id.ToString(),
                        color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null)).ToList());

            var sanInput = await input.RequestString("Enter a comma separated list of SiteIds or 'S' for all sites");
            sites = ProcessSiteIds(ret, sites, sanInput);
            if (sites != null)
            {
                var hosts = sites.SelectMany(x => x.Hosts).Distinct().OrderBy(x => x);
                if (await _optionsHelper.AquireAdvancedOptions(input, hosts, runLevel, ret))
                {
                    return ret;
                }
            }
            return null;
        }

        public override Task<IISSitesOptions> Default()
        {
            var ret = new IISSitesOptions();
            var args = _arguments.GetArguments<IISSiteArguments>();
            var sites = _siteHelper.GetSites(false);
            var rawSiteIds = _arguments.TryGetRequiredArgument(nameof(args.SiteId), args.SiteId);
            sites = ProcessSiteIds(ret, sites, rawSiteIds);
            if (sites != null)
            {
                if (_optionsHelper.DefaultAdvancedOptions(args, sites.SelectMany(s => s.Hosts), ret))
                {
                    return Task.FromResult(ret);
                }
            }
            return Task.FromResult(default(IISSitesOptions));
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
