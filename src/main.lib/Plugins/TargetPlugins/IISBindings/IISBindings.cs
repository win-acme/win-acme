using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindings : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISBindingsOptions _options;
        private readonly IISBindingHelper _helper;
        private readonly UserRoleService _userRoleService;

        public IISBindings(
            ILogService logService, UserRoleService roleService,
            IISBindingHelper helper, IISBindingsOptions options)
        {
            _log = logService;
            _options = options;
            _helper = helper;
            _userRoleService = roleService;
        }

        internal static string PatternToRegex(string pattern) =>
            $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

        internal static string HostsToRegex(IEnumerable<string> hosts) =>
            $"^({string.Join('|', hosts.Select(x => Regex.Escape(x)))})$";

        private Regex? GetRegex(IISBindingsOptions options)
        {
            if (!string.IsNullOrEmpty(options.IncludePattern))
            {
                return new Regex(PatternToRegex(options.IncludePattern));
            }
            if (options.IncludeHosts != null && options.IncludeHosts.Any())
            {
                return new Regex(HostsToRegex(options.IncludeHosts));
            }
            return options.IncludeRegex;
        }

        internal static bool Matches(IISBindingHelper.IISBindingOption binding, Regex regex)
        {
            return regex.IsMatch(binding.HostUnicode)
                || regex.IsMatch(binding.HostPunycode);
        }

        public async Task<Target?> Generate()
        {
            // Check if we have any bindings
            var bindings = _helper.GetBindings();
            _log.Verbose("{0} named bindings found in IIS", bindings.Count());

            var friendlyNameSuggestion = "[IIS]";

            // Filter by site
            if (_options.IncludeSiteIds != null && _options.IncludeSiteIds.Any())
            {
                var sites = string.Join(',', _options.IncludeSiteIds);
                _log.Debug("Filtering by site {0}", sites);
                bindings = bindings.Where(x => _options.IncludeSiteIds.Contains(x.SiteId)).ToList();
                friendlyNameSuggestion += $" site {sites}";
                _log.Verbose("{0} bindings remaining after site filter", bindings.Count());
            } 
            else
            {
                _log.Verbose("No site filter applied");
                friendlyNameSuggestion += $" all sites";
            }

            // Filter by pattern
            var regex = GetRegex(_options);
            if (regex != null)
            {
                _log.Debug("Filtering by host: {regex}", regex);
                friendlyNameSuggestion += $" {regex}";
                bindings = bindings.Where(x => Matches(x, regex)).ToList();
                _log.Verbose("{0} bindings remaining after host filter", bindings.Count());
            }
            else
            {
                _log.Verbose("No host filter applied");
                friendlyNameSuggestion += $" all hosts";
            }

            // Remove exlusions
            if (_options.ExcludeHosts != null && _options.ExcludeHosts.Any())
            {
                bindings = bindings.Where(x => _options.ExcludeHosts.Contains(x.HostUnicode)).ToList();
                _log.Verbose("{0} named bindings remaining after explicit exclusions", bindings.Count());
            }

            // Check if we have anything left
            if (!bindings.Any())
            {
                _log.Error("No usable bindings found");
                return null;
            }

            var result = new Target()
            {
                FriendlyName = friendlyNameSuggestion,
                CommonName = _options.CommonName ?? bindings.First().HostUnicode,
                Parts = bindings.
                    GroupBy(x => x.SiteId).
                    Select(group => new TargetPart
                    {
                        SiteId = group.Key,
                        Identifiers = group.Select(x => x.HostUnicode).ToList()
                    }).
                    ToList()
            };
            return result;
        }

        bool IPlugin.Disabled => Disabled(_userRoleService);
        internal static bool Disabled(UserRoleService userRoleService) => !userRoleService.AllowIIS;
    }
}
