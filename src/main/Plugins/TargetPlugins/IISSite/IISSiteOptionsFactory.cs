using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Globalization;
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

        public override IISSiteOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new IISSiteOptions();
            var sites = _helper.
                GetSites(arguments.MainArguments.HideHttps, true).
                Where(x => x.Hidden == false).
                Where(x => x.Hosts.Any());
            if (!sites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            var chosen = inputService.ChooseFromList("Choose site",
                sites, 
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
                        chosen.Hosts.Except(ret.ExcludeBindings ?? new List<string>()),
                        x => new Choice<string>(x), 
                        true);
                }
                return ret;
            }
            return null;
        }

        public override IISSiteOptions Default(IArgumentsService arguments)
        {
            var ret = new IISSiteOptions();
            var args = arguments.GetArguments<IISSiteArguments>();
            var rawSiteId = arguments.TryGetRequiredArgument(nameof(args.SiteId), args.SiteId);
            if (long.TryParse(rawSiteId, out long siteId))
            {
                var site = _helper.GetSites(false, false).FirstOrDefault(binding => binding.Id == siteId);
                if (site != null)
                {
                    ret.SiteId = site.Id;
                    ret.ExcludeBindings = args.ExcludeBindings.ParseCsv();
                    if (ret.ExcludeBindings != null)
                    {
                        ret.ExcludeBindings = ret.ExcludeBindings.Select(x => x.ConvertPunycode()).ToList();
                    }
                   
                    ret.FriendlyNameSuggestion = $"Site-{ret.SiteId}";
                    var commonName = args.CommonName;
                    if (!string.IsNullOrWhiteSpace(commonName))
                    {
                        commonName = commonName.ToLower().Trim().ConvertPunycode();
                        if (site.Hosts.Contains(commonName) && 
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
                else
                {
                    _log.Error("Unable to find SiteId {siteId}", siteId);
                }
            }
            else
            {
                _log.Error("Invalid SiteId {siteId}", args.SiteId);
            }
            return null;
        }
    }
}
