using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSite : ITargetPlugin
    {
        protected ILogService _log;
        protected IIISClient _iisClient;
        protected IISSiteHelper _helper;
        protected IISSiteOptions _options;

        public IISSite(ILogService logService, IIISClient iisClient, IISSiteHelper helper, IISSiteOptions options)
        {
            _log = logService;
            _iisClient = iisClient;
            _helper = helper;
            _options = options;
        }

        public Target Generate()
        {
            var site = _helper.GetSites(false, false).FirstOrDefault(s => s.Id == _options.SiteId);
            if (site == null)
            {
                _log.Error($"SiteId {_options.SiteId} not found");
                return null;
            }
            var hosts = site.Hosts;
            if (_options.ExcludeBindings != null)
            {
                hosts = hosts.Except(_options.ExcludeBindings).ToList();
            }
            var validCommonName = !string.IsNullOrEmpty(_options.CommonName) && hosts.Contains(_options.CommonName);
            if (!validCommonName)
            {
                _log.Warning($"Specified common name {_options.CommonName} not valid");
            }
            return new Target()
            {
                CommonName = validCommonName ? _options.CommonName : hosts.FirstOrDefault(),
                Parts = new[] {
                    new TargetPart() {
                        Hosts = hosts,
                        SiteId = site.Id
                    }
                }
            };
        }
    }
}