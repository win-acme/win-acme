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
            if (!string.IsNullOrEmpty(options.Simple))
            {
                return new Regex(IISBindingsOptionsFactory.WildcardToRegex(options.Simple));
            }

            if (options.RegEx != default)
            {
                return options.RegEx;
            }

            return default;
        }

        private bool Matches(IISBindingHelper.IISBindingOption binding, Regex regex)
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
                FriendlyName = $"[{nameof(IISBindings)}] {(string.IsNullOrEmpty(_options.Simple) ? nameof(_options.RegEx) : nameof(_options.Simple))}",
                CommonName = matchingBindings.First().HostUnicode,
                Parts = matchingBindings.Select(binding => new TargetPart { SiteId = binding.SiteId, Identifiers = new List<string> { binding.HostUnicode } }).ToList()
            });
        }

        bool IPlugin.Disabled => Disabled(_userRoleService);
        internal static bool Disabled(UserRoleService userRoleService) => !userRoleService.AllowIIS;
    }
}
