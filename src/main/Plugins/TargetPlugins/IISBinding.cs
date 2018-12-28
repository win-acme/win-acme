using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBinding : ITargetPlugin
    {
        private readonly ILogService _log;
        private IISClient _iisClient;

        public IISBinding(ILogService logService, IISClient iisClient)
        {
            _iisClient = iisClient;
            _log = logService;
        }

        public Target Generate(TargetPluginOptions options)
        {
            var bindingOptions = (IISBindingOptions)options;
            return new Target()
            {
                CommonName = bindingOptions.Host,
                Parts = new[] {
                    new TargetPart {
                        Hosts = new[] { bindingOptions.Host },
                        SiteId = bindingOptions.SiteId
                    }
                }
            };
        }

        public IEnumerable<Target> Split(Target scheduled)
        {
            return new List<Target> { scheduled };
        }
    }
}
