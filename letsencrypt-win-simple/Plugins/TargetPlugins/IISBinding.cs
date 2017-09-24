using LetsEncrypt.ACME.Simple.Services;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Clients;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISBinding : IISClient, ITargetPlugin
    {
        string IHasName.Name => nameof(IISBinding);
        string IHasName.Description => "Single binding of an IIS site";
        Target ITargetPlugin.Default(Options options) => null;

        Target ITargetPlugin.Aquire(Options options, InputService input)
        {
            return input.ChooseFromList("Choose site",
                GetBindings(options, true).Where(x => x.Hidden == false),
                x => InputService.Choice.Create(x, description: $"{x.Host} (SiteId {x.SiteId}) [@{x.WebRootPath}]"),
                true);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetBindings(options, false).FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null) {
                UpdateWebRoot(scheduled, match);
                return scheduled;
            }
            return null;
        }

        private List<Target> GetBindings(Options options, bool logInvalidSites)
        {
            if (GetServerManager() == null) {
                Program.Log.Warning("IIS not found. Skipping scan.");
                return new List<Target>();
            }

            // Get all bindings matched together with their respective sites
            Program.Log.Debug("Scanning IIS site bindings for hosts");
            var siteBindings = GetServerManager().Sites.
                SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host));

            // Option: hide http bindings when there are already https equivalents
            var hidden = siteBindings.Take(0);
            if (options.HideHttps) {
                hidden = siteBindings.
                    Where(sb => sb.binding.Protocol == "https" ||
                                sb.site.Bindings.Any(other => other.Protocol == "https" &&
                                                                string.Equals(sb.binding.Host, other.Host, StringComparison.InvariantCultureIgnoreCase)));
            }

            var targets = siteBindings.
                Select(sb => new {
                    idn = IdnMapping.GetAscii(sb.binding.Host.ToLower()),
                    sb.site,
                    sb.binding,
                    hidden = hidden.Contains(sb)
                }).
                Select(sbi => new Target {
                    SiteId = sbi.site.Id,
                    Host = sbi.idn,
                    HostIsDns = true,
                    Hidden = sbi.hidden,
                    IIS = true,
                    WebRootPath = sbi.site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                    PluginName = PluginName
                }).
                DistinctBy(t => t.Host).
                OrderBy(t => t.SiteId).
                ToList();

            if (targets.Count() == 0 && logInvalidSites) {
                Program.Log.Warning("No IIS bindings with host names were found. A host name is required to verify domain ownership.");
            }
            return targets;
        }
    }
}
