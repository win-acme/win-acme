using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSiteFactory : BaseTargetPluginFactory<IISSite>
    {
        public IISSiteFactory() : base(nameof(IISSite), "SAN certificate for all bindings of an IIS site") { }
    }

    class IISSite : ITargetPlugin
    {
        protected ILogService _log;
        protected IISClient _iisClient;

        public IISSite(ILogService logService, IISClient iisClient)
        {
            _log = logService;
            _iisClient = iisClient;
        }

        Target ITargetPlugin.Default(IOptionsService optionsService)
        {
            var rawSiteId = optionsService.TryGetRequiredOption(nameof(optionsService.Options.SiteId), optionsService.Options.SiteId);
            long siteId = 0;
            if (long.TryParse(rawSiteId, out siteId))
            {
                var found = GetSites(false, false).FirstOrDefault(binding => binding.SiteId == siteId);
                if (found != null)
                {
                    found.ExcludeBindings = optionsService.Options.ExcludeBindings;
                    return found;
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

        Target ITargetPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            var chosen = inputService.ChooseFromList("Choose site",
                GetSites(optionsService.Options.HideHttps, true).Where(x => x.Hidden == false),
                x => new Choice<Target>(x) { Description = x.Host },
                true);
            if (chosen != null)
            {
                // Exclude bindings 
                inputService.WritePagedList(chosen.AlternativeNames.Select(x => Choice.Create(x, "")));
                chosen.ExcludeBindings = inputService.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions");
                return chosen;
            }
            return null;
        }

        Target ITargetPlugin.Refresh(Target scheduled)
        {
            var match = GetSites(false, false).FirstOrDefault(binding => binding.SiteId == scheduled.SiteId);
            if (match != null)
            {
                _iisClient.UpdateWebRoot(scheduled, match);
                _iisClient.UpdateAlternativeNames(scheduled, match);
                return scheduled;
            }
            _log.Error("SiteId {id} not found", scheduled.SiteId);
            return null;
        }

        internal List<Target> GetSites(bool hideHttps, bool logInvalidSites)
        {
            if (_iisClient.ServerManager == null) {
                _log.Warning("IIS not found. Skipping scan.");
                return new List<Target>();
            }

            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS sites");
            var sites = _iisClient.ServerManager.Sites.AsEnumerable();

            // Option: hide http bindings when there are already https equivalents
            var hidden = sites.Take(0);
            if (hideHttps)
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
                    PluginName = IISInstallerFactory.PluginName,
                    IIS = true,
                    AlternativeNames = GetHosts(site)
                }).
                Where(target => {
                    if (target.AlternativeNames.Count > SettingsService.maxNames) {
                        if (logInvalidSites) {
                            _log.Information("{site} has too many hosts for a single certificate. Let's Encrypt has a maximum of {maxNames}.", target.Host, SettingsService.maxNames);
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
                OrderBy(target => target.Host).
                ToList();

            if (targets.Count() == 0 && logInvalidSites) {
                _log.Warning("No applicable IIS sites were found.");
            }
            return targets;
        }

        private List<string> GetHosts(Site site) {
            return site.Bindings.Select(x => x.Host.ToLower()).
                            Where(x => !string.IsNullOrWhiteSpace(x)).
                            Select(x => _iisClient.IdnMapping.GetAscii(x)).
                            Distinct().
                            ToList();
        }

        public virtual IEnumerable<Target> Split(Target scheduled)
        {
            return new List<Target> { scheduled };
        }

    }
}