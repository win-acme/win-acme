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
      
        Target ITargetPlugin.Default(OptionsService options)
        {
            var rawSiteId = options.TryGetRequiredOption(nameof(options.Options.SiteId), options.Options.SiteId);
            long siteId = 0;
            if (long.TryParse(rawSiteId, out siteId))
            {
                var found = GetSites(options.Options, false).FirstOrDefault(binding => binding.SiteId == siteId);
                if (found != null)
                {
                    found.ExcludeBindings = options.Options.ExcludeBindings;
                    return found;
                }
                else
                {
                    _log.Error("Unable to find SiteId {siteId}", siteId);
                }
            }
            else
            {
                _log.Error("Invalid SiteId {siteId}", options.Options.SiteId);
            }
            return null;
        }

        Target ITargetPlugin.Aquire(OptionsService options, InputService input)
        {
            var chosen = input.ChooseFromList("Choose site",
                GetSites(options.Options, true).Where(x => x.Hidden == false),
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

        Target ITargetPlugin.Refresh(OptionsService options, Target scheduled)
        {
            var match = GetSites(options.Options, false).FirstOrDefault(binding => binding.SiteId == scheduled.SiteId);
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
                _log.Warning("IIS not found. Skipping scan.");
                return new List<Target>();
            }

            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS sites");
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
                            _log.Information("{site} has too many hosts for a single certificate. Let's Encrypt has a maximum of {maxNames}.", target.Host, Settings.maxNames);
                        }
                        return false;
                    } else if (target.AlternativeNames.Count == 0) {
                        if (logInvalidSites) {
                            _log.Information("No valid hosts found for {site}.", target.Host);
                        }
                        return false;
                    }
                    return true;
                }).
                OrderBy(target => target.SiteId).
                ToList();

            if (targets.Count() == 0 && logInvalidSites) {
                _log.Warning("No applicable IIS sites were found.");
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