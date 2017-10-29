using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSites : IISSite, ITargetPlugin
    {
        string IHasName.Name => nameof(IISSites);
        string IHasName.Description => "SAN certificate for all bindings of multiple IIS sites";

        Target ITargetPlugin.Default(IOptionsService options) {
            var rawSiteId = options.TryGetRequiredOption(nameof(options.Options.SiteId), options.Options.SiteId);
            var totalTarget = GetCombinedTarget(GetSites(options.Options, false), rawSiteId);
            totalTarget.ExcludeBindings = options.Options.ExcludeBindings;
            return totalTarget;
        }

        Target GetCombinedTarget(List<Target> targets, string sanInput)
        {
            List<Target> siteList = new List<Target>();
            if (sanInput == "s")
            {
                siteList.AddRange(targets);
            }
            else
            {
                string[] siteIDs = sanInput.Trim().Trim(',').Split(',').Distinct().ToArray();
                foreach (var idString in siteIDs)
                {
                    int id = -1;
                    if (int.TryParse(idString, out id))
                    {
                        var site = targets.Where(t => t.SiteId == id).FirstOrDefault();
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
            Target totalTarget = new Target
            {
                PluginName = IISSiteServerPlugin.PluginName,
                Host = string.Join(",", siteList.Select(x => x.SiteId)),
                HostIsDns = false,
                IIS = true,
                WebRootPath = "x", // prevent FileSystem
                AlternativeNames = siteList.SelectMany(x => x.AlternativeNames).Distinct().ToList()
            };
            return totalTarget;
        }

        Target ITargetPlugin.Aquire(IOptionsService options, IInputService input)
        {
            List<Target> targets = GetSites(options.Options, true).Where(x => x.Hidden == false).ToList();
            input.WritePagedList(targets.Select(x => Choice.Create(x, $"{x.Host} ({x.AlternativeNames.Count()} bindings) [@{x.WebRootPath}]", x.SiteId.ToString())).ToList());
            var sanInput = input.RequestString("Enter a comma separated list of site IDs, or 'S' to run for all sites").ToLower().Trim();
            var totalTarget = GetCombinedTarget(targets, sanInput);
            input.WritePagedList(totalTarget.AlternativeNames.Select(x => Choice.Create(x, "")));
            totalTarget.ExcludeBindings = input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");
            return totalTarget;
        }

        Target ITargetPlugin.Refresh(IOptionsService options, Target scheduled)
        {
            // TODO: check if the sites still exist, log removed sites
            // and return null if none of the sites can be found (cancel
            // the renewal of the certificate). Maybe even save the "S"
            // switch somehow to add sites if new ones are added to the 
            // server.
            return scheduled;
        }
    }
}