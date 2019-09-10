using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBinding : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISBindingOptions _options;
        private readonly IISBindingHelper _helper;

        public IISBinding(ILogService logService, IISBindingHelper helper, IISBindingOptions options)
        {
            _log = logService;
            _options = options;
            _helper = helper;
        }

        public Task<Target> Generate()
        {
            var allBindings = _helper.GetBindings(false);
            var matchingBindings = allBindings.Where(x => x.HostUnicode == _options.Host);
            if (matchingBindings.Count() == 0)
            {
                _log.Error("Binding {binding} not yet found in IIS, create it or use the Manual target plugin instead", _options.Host);
                return null;
            }
            else if (!matchingBindings.Any(b => b.SiteId == _options.SiteId))
            {
                var newMatch = matchingBindings.First();
                _log.Warning("Binding {binding} moved from site {a} to site {b}", _options.Host, _options.SiteId, newMatch.SiteId);
                _options.SiteId = newMatch.SiteId;
            }
            return Task.FromResult(new Target()
            {
                FriendlyName = $"[{nameof(IISBinding)}] {_options.Host}",
                CommonName = _options.Host,
                Parts = new[] {
                    new TargetPart {
                        Identifiers = new List<string> { _options.Host },
                        SiteId = _options.SiteId
                    }
                }
            });
        }
    }
}
