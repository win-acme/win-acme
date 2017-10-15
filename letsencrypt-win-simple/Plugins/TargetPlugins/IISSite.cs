using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using static LetsEncrypt.ACME.Simple.Services.InputService;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSite : IISClient, ITargetPlugin
    {
        string IHasName.Name => nameof(IISSite);
        string IHasName.Description => "SAN certificate for all bindings of an IIS site";
      
        Target ITargetPlugin.Default(Options options)
        {
            if (!string.IsNullOrEmpty(options.SiteId))
            {
                long siteId = 0;
                if (long.TryParse(options.SiteId, out siteId))
                {
                    var found = GetSites(options, false).FirstOrDefault(binding => binding.SiteId == siteId);
                    if (found != null)
                    {
                        found.ExcludeBindings = options.ExcludeBindings;
                        return found;
                    }
                    else
                    {
                        Program.Log.Error("Unable to find site with id {siteId}", siteId);
                    }
                }
                else
                {
                    Program.Log.Error("Invalid SiteId {siteId}", options.SiteId);
                }
            }
            else
            {
                Program.Log.Error("Please specify the --siteid argument");
            }
            return null;
        }

        Target ITargetPlugin.Aquire(Options options, InputService input)
        {
            var chosen = input.ChooseFromList("Choose site",
                GetSites(options, true).Where(x => x.Hidden == false),
                x => new Choice<Target>(x) { description = x.Host },
                true);
            if (chosen != null)
            {
                // Exclude bindings 
                input.WritePagedList(chosen.AlternativeNames.Select(x => Choice.Create(x, "")));
                chosen.ExcludeBindings = input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");
                return chosen;
            }
            return null;
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetSites(options, false).FirstOrDefault(binding => binding.SiteId == scheduled.SiteId);
            if (match != null)
            {
                UpdateWebRoot(scheduled, match);
                UpdateAlternativeNames(scheduled, match);
                return scheduled;
            }
            return null;
        }

        internal List<Target> GetSites(Options options, bool logInvalidSites)
        {
            if (ServerManager == null) {
                Program.Log.Warning("IIS not found. Skipping scan.");
                return new List<Target>();
            }

            // Get all bindings matched together with their respective sites
            Program.Log.Debug("Scanning IIS sites");
            var sites = ServerManager.Sites.AsEnumerable();

            // Option: hide http bindings when there are already https equivalents
            var hidden = sites.Take(0);
            if (options.HideHttps)
            {
                hidden = sites.Where(site => site.Bindings.
                    All(binding => binding.Protocol == "https" ||
                                    site.Bindings.Any(other => other.Protocol == "https" &&
                                                                string.Equals(other.Host, binding.Host, StringComparison.InvariantCultureIgnoreCase))));
            }

            var targets = sites.
                Select(site => new Target {
                    SiteId = site.Id,
                    Host = site.Name,
                    HostIsDns = false,
                    Hidden = hidden.Contains(site),
                    WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                    PluginName = PluginName,
                    IIS = true,
                    AlternativeNames = GetHosts(site)
                }).
                Where(target => {
                    if (target.AlternativeNames.Count > Settings.maxNames) {
                        if (logInvalidSites) {
                            Program.Log.Information("{site} has too many hosts for a single certificate. Let's Encrypt has a maximum of {maxNames}.", target.Host, Settings.maxNames);
                        }
                        return false;
                    } else if (target.AlternativeNames.Count == 0) {
                        if (logInvalidSites) {
                            Program.Log.Information("No valid hosts found for {site}.", target.Host);
                        }
                        return false;
                    }
                    return true;
                }).
                OrderBy(target => target.SiteId).
                ToList();

            if (targets.Count() == 0 && logInvalidSites) {
                Program.Log.Warning("No applicable IIS sites were found.");
            }
            return targets;
        }

        private List<string> GetHosts(Site site) {
            return site.Bindings.Select(x => x.Host.ToLower()).
                            Where(x => !string.IsNullOrWhiteSpace(x)).
                            Select(x => IdnMapping.GetAscii(x)).
                            Distinct().
                            ToList();
        }

    }
}