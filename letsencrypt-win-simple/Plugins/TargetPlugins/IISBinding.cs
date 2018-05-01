using PKISharp.WACS.Clients;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingFactory : BaseTargetPluginFactory<IISBinding>
    {
        public override bool Hidden => _iisClient.Version.Major == 0;
        protected IISClient _iisClient;

        public IISBindingFactory(ILogService log, IISClient iisClient) : 
            base(log, nameof(IISBinding), "Single binding of an IIS site")
        {
            _iisClient = iisClient;
        }
    }

    internal class IISBinding : ITargetPlugin
    {
        private ILogService _log;
        private IISClient _iisClient;

        public IISBinding(ILogService logService, IISClient iisClient)
        {
            _iisClient = iisClient;
            _log = logService;
        }

        Target ITargetPlugin.Default(IOptionsService optionsService)  
        {
            var hostName = optionsService.TryGetRequiredOption(nameof(optionsService.Options.ManualHost), optionsService.Options.ManualHost);
            var rawSiteId = optionsService.Options.SiteId;
            long siteId = 0;
            var filterSet = GetBindings(false, false);
            if (long.TryParse(rawSiteId, out siteId))
            {
                filterSet = filterSet.Where(x => x.TargetSiteId == siteId).ToList();
            }
            return filterSet.
                Where(x => x.Host == hostName).
                FirstOrDefault();
        }

        Target ITargetPlugin.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return inputService.ChooseFromList("Choose site",
                GetBindings(optionsService.Options.HideHttps, true).Where(x => x.Hidden == false),
                x => Choice.Create(x, description: $"{x.Host} (SiteId {x.TargetSiteId}) [@{x.WebRootPath}]"),
                true);
        }

        Target ITargetPlugin.Refresh(Target scheduled)
        {
            var match = GetBindings(false, false).FirstOrDefault(binding => string.Equals(binding.Host, scheduled.Host, StringComparison.InvariantCultureIgnoreCase));
            if (match != null) {
                return scheduled;
            }
            _log.Error("Binding {host} not found", scheduled.Host);
            return null;
        }

        private List<Target> GetBindings(bool hideHttps, bool logInvalidSites)
        {
            if (_iisClient.ServerManager == null) {
                _log.Warning("IIS not found. Skipping scan.");
                return new List<Target>();
            }

            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS site bindings for hosts");
            var siteBindings = _iisClient.WebSites.
                SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host)).
                Where(sb => !sb.binding.Host.StartsWith("*"));

            // Option: hide http bindings when there are already https equivalents
            var hidden = siteBindings.Take(0);
            if (hideHttps) {
                hidden = siteBindings.
                    Where(sb => sb.binding.Protocol == "https" ||
                                sb.site.Bindings.Any(other => other.Protocol == "https" &&
                                                                string.Equals(sb.binding.Host, other.Host, StringComparison.InvariantCultureIgnoreCase)));
            }

            var targets = siteBindings.
                Select(sb => new {
                    idn = _iisClient.IdnMapping.GetAscii(sb.binding.Host.ToLower()),
                    sb.site,
                    sb.binding,
                    hidden = hidden.Contains(sb)
                }).
                Select(sbi => new Target {
                    TargetSiteId = sbi.site.Id,
                    Host = sbi.idn,
                    HostIsDns = true,
                    Hidden = sbi.hidden,
                    IIS = true,
                    WebRootPath = sbi.site.WebRoot()
                }).
                DistinctBy(t => t.Host).
                OrderBy(t => t.Host).
                ToList();

            if (targets.Count() == 0 && logInvalidSites) {
                _log.Warning("No IIS bindings with host names were found. A host name is required to verify domain ownership.");
            }
            return targets;
        }

        public IEnumerable<Target> Split(Target scheduled)
        {
            return new List<Target> { scheduled };
        }
    }
}
