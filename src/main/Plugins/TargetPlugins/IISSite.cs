using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSite : ITargetPlugin
    {
        protected ILogService _log;
        protected IISClient _iisClient;
        protected IISSiteHelper _helper;

        public IISSite(ILogService logService, IISClient iisClient, IISSiteHelper helper)
        {
            _log = logService;
            _iisClient = iisClient;
            _helper = helper;
        }

        public Target Generate(TargetPluginOptions options)
        {
            var iisOptions = (IISSiteOptions)options;
            return new Target();
        }
    }
}