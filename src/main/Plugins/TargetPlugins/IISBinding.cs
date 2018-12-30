using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

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
