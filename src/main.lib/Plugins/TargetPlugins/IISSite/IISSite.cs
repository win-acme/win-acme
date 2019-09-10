using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSite : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IISSiteHelper _helper;
        private readonly IISSiteOptions _options;

        public IISSite(ILogService logService, IISSiteHelper helper, IISSiteOptions options)
        {
            _log = logService;
            _helper = helper;
            _options = options;
        }

        public Task<Target> Generate()
        {
            var site = _helper.GetSites(false, false).FirstOrDefault(s => s.Id == _options.SiteId);
            if (site == null)
            {
                _log.Error($"SiteId {_options.SiteId} not found");
                return Task.FromResult(default(Target));
            }
            var hosts = site.Hosts;
            if (_options.ExcludeBindings != null)
            {
                hosts = hosts.Except(_options.ExcludeBindings).ToList();
            }
            var cn = _options.CommonName;
            var cnDefined = !string.IsNullOrWhiteSpace(cn);
            var cnValid = cnDefined && hosts.Contains(cn);
            if (cnDefined && !cnValid)
            {
                _log.Warning("Specified common name {cn} not valid", cn);
            }
            return Task.FromResult(new Target()
            {
                FriendlyName = $"[{nameof(IISSite)}] {site.Name}",
                CommonName = cnValid ? cn : hosts.FirstOrDefault(),
                Parts = new[] {
                    new TargetPart() {
                        Identifiers = hosts,
                        SiteId = site.Id
                    }
                }
            });
        }
    }
}