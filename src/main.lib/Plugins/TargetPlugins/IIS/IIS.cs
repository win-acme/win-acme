using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [IPlugin.Plugin<IISSitesOptions, IISOptionsFactory>
        ("cdd79a68-4a87-4039-bee8-5a0ebdca41cb", "IISSites", "Read sites from IIS (legacy)", Hidden = true)]
    [IPlugin.Plugin<IISSiteOptions, IISOptionsFactory>
        ("d7940b23-f570-460e-ab15-2c822a79009b", "IISSite", "Read site from IIS (legacy)", Hidden = true)]
    [IPlugin.Plugin<IISBindingOptions, IISOptionsFactory>
        ("2f5dd428-0f5d-4c8a-8fd0-56fc1b5985ce", "IISBinding", "Read bindings from IIS (legacy)", Hidden = true)]
    [IPlugin.Plugin<IISOptions, IISOptionsFactory>
        ("54deb3ee-b5df-4381-8485-fe386054055b", "IIS", "Read bindings from IIS")]
    internal class IIS : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISOptions _options;
        private readonly IISHelper _helper;
        private readonly IUserRoleService _userRoleService;

        public IIS(
            ILogService logService, IUserRoleService roleService,
            IISHelper helper, IISOptions options)
        {
            _log = logService;
            _options = options;
            _helper = helper;
            _userRoleService = roleService;
        }

        public async Task<Target> Generate()
        {
            // Check if we have any bindings
            var allBindings = _helper.GetBindings();
            var filteredBindings = _helper.FilterBindings(allBindings, _options);
            if (filteredBindings.Count == 0)
            {
                _log.Error("No bindings matched, unable to proceed");
                return new NullTarget();
            }

            // Handle common name
            var cn = _options.CommonName ?? "";
            var cnDefined = !string.IsNullOrWhiteSpace(cn);
            var cnBinding = default(IISHelper.IISBindingOption); 
            if (cnDefined)
            {
                cnBinding = filteredBindings.FirstOrDefault(x => x.HostUnicode == cn);
            }
            var cnValid = cnDefined && cnBinding != null;
            if (cnDefined && !cnValid)
            {
                _log.Warning("Specified common name {cn} not valid", cn);
            }

            // Generate friendly name suggestion
            var friendlyNameSuggestion = "[IIS]";
            if (_options.IncludeSiteIds != null && _options.IncludeSiteIds.Any())
            {
                var sites = _helper.GetSites(false);
                var site = default(IISHelper.IISSiteOption);
                if (cnBinding != null)
                {
                    site = sites.FirstOrDefault(s => s.Id == cnBinding.SiteId);
                } 
                if (site == null)
                {
                    site = sites.FirstOrDefault(x => _options.IncludeSiteIds.Contains(x.Id));
                }
                var count = _options.IncludeSiteIds.Count;
                if (site != null)
                {
                    friendlyNameSuggestion += $" {site.Name}";
                    count -= 1;
                }
                if (count > 0)
                {
                    friendlyNameSuggestion += $" (+{count} other{(count == 1 ? "" : "s")})";
                } 
            }
            else
            {
                friendlyNameSuggestion += $" (any site)";
            }

            if (!string.IsNullOrEmpty(_options.IncludePattern))
            {
                friendlyNameSuggestion += $" | {_options.IncludePattern}";
            }
            else if (_options.IncludeHosts != null && _options.IncludeHosts.Any())
            {
                var host = default(string);
                if (cnBinding != null)
                {
                    host = cnBinding.HostUnicode;
                }
                if (host == null)
                {
                    host = _options.IncludeHosts.First();
                }
                friendlyNameSuggestion += $", {host}";
                var count = _options.IncludeHosts.Count;
                if (count > 1)
                {
                    friendlyNameSuggestion += $" (+{count - 1} other{(count == 2 ? "" : "s")})";
                }
            }
            else if (_options.IncludeRegex != null)
            {
                friendlyNameSuggestion += $", {_options.IncludeRegex}";
            }
            else
            {
                friendlyNameSuggestion += $", (any host)";
            }

            // Return result
            var commonName = cnValid ? cn : filteredBindings.First().HostUnicode;
            var parts = filteredBindings.
                GroupBy(x => x.SiteId).
                Select(group => new TargetPart(group.Select(x => new DnsIdentifier(x.HostUnicode)))
                {
                    SiteId = group.Key,
                    SiteType = group.First().SiteType
                });
            return new Target(friendlyNameSuggestion, commonName, parts.ToList());
        }

        (bool, string?) IPlugin.Disabled => Disabled(_userRoleService);

        internal static (bool, string?) Disabled(IUserRoleService userRoleService) 
        {
            var (allow, reason) = userRoleService.AllowIIS;
            if (!allow)
            {
                return (true, reason);
            } 
            else
            {
                return (false, null);
            }
        }
    }
}
