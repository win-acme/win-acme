using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBinding : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly IIISClient _iisClient;
        private IISBindingOptions _options;
        private readonly IISBindingHelper _helper;

        public IISBinding(ILogService logService, IIISClient iisClient, IISBindingHelper helper, IISBindingOptions options)
        {
            _iisClient = iisClient;
            _log = logService;
            _options = options;
            _helper = helper;
        }

        public Target Generate()
        {
            var bindings = _helper.GetBindings(false, false);
            var binding = bindings.FirstOrDefault(x => x.HostUnicode == _options.Host);
            if (binding == null)
            {
                _log.Error("Binding {binding} no longer found in IIS", _options.Host);
                return null;
            }
            if (binding.SiteId != _options.SiteId)
            {
                _log.Warning("Binding {binding} moved from site {a} to site {b}", _options.SiteId, binding.SiteId);
                _options.SiteId = binding.SiteId;
            }
            return new Target()
            {
                CommonName = _options.Host,
                Parts = new[] {
                    new TargetPart {
                        Hosts = new[] { _options.Host },
                        SiteId = _options.SiteId
                    }
                }
            };
        }
    }
}
