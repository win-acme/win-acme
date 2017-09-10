using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System.Collections.Generic;
using System.Linq;
using static LetsEncrypt.ACME.Simple.Services.InputService;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSites : IISPlugin, ITargetPlugin
    {
        string IHasName.Name
        {
            get
            {
                return nameof(IISSites);
            }
        }

        string ITargetPlugin.Description
        {
            get
            {
                return "All bindings for multiple IIS sites";
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            List<Target> targets = GetSites();
            List<Target> siteList = new List<Target>();
            Program.Input.WritePagedList(targets.Select(x => Choice.Create(x, $"{x.Host} ({x.AlternativeNames.Count()} bindings) [@{x.WebRootPath}]", x.SiteId.ToString())).ToList());
            var sanInput = Program.Input.RequestString("Enter a comma separated list of site IDs, or 'S' to run for all sites").ToLower().Trim();
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
                            Program.Log.Warning($"SiteId '{idString}' not found");
                        }
                    }
                    else
                    {
                        Program.Log.Warning($"Invalid SiteId '{idString}', should be a number");
                    }
                }
                if (siteList.Count == 0)
                {
                    Program.Log.Warning($"No valid sites selected");
                    return null;
                }
            }
            Target totalTarget = new Target();
            totalTarget.PluginName = IISSiteServerPlugin.PluginName;
            totalTarget.Host = string.Join(",", siteList.Select(x => x.SiteId));
            totalTarget.HostIsDns = false;
            totalTarget.AlternativeNames = siteList.SelectMany(x => x.AlternativeNames).Distinct().ToList();

            Program.Input.WritePagedList(totalTarget.AlternativeNames.Select(x => Choice.Create(x, "")));
            totalTarget.ExcludeBindings = Program.Input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");

            return totalTarget;
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
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