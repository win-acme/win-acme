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

        private Regex GetRegex(IISBindingsOptions options)
        {
            if (!string.IsNullOrEmpty(options.Pattern))
            {
                return new Regex(IISBindingsOptionsFactory.PatternToRegex(options.Pattern));
            }
            if (!string.IsNullOrEmpty(options.Hosts))
            {
                return new Regex(IISBindingsOptionsFactory.HostsToRegex(options.Hosts));
            }
            return options.Regex;
        }

        internal static bool Matches(IISBindingHelper.IISBindingOption binding, Regex regex)
        {
            return regex.IsMatch(binding.HostUnicode)
                || regex.IsMatch(binding.HostPunycode);
        }

        public Task<Target> Generate()
        {
            var allBindings = _helper.GetBindings();
            var regex = GetRegex(_options);

            if (regex == default)
            {
                _log.Error("No search term defined within the options.");
                return Task.FromResult(default(Target));
            }

            var matchingBindings = allBindings.Where(x => Matches(x, regex));
            if (!matchingBindings.Any())
            {
                _log.Error("Binding with {search} not yet found in IIS, create it or use the Manual target plugin instead", regex.ToString());
                return Task.FromResult(default(Target));
            }

            return Task.FromResult(new Target()
            {
                FriendlyName = $"[{nameof(IISBindings)}] {_options.Pattern ?? _options.Regex?.ToString() ?? _options.Hosts}",
                CommonName = matchingBindings.First().HostUnicode,
                Parts = matchingBindings.
                    GroupBy(x => x.SiteId).
                    Select(group => new TargetPart {
                        SiteId = group.Key,
                        Identifiers = group.Select(x => x.HostUnicode).ToList()
                    }).
                    ToList()
            });
        }

        bool IPlugin.Disabled => Disabled(_userRoleService);
        internal static bool Disabled(UserRoleService userRoleService) => !userRoleService.AllowIIS;
    }
}
