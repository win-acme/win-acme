using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSites : ITargetPlugin
    {
        protected ILogService _log;
        protected IISClient _iisClient;
        protected IISSiteHelper _helper;

        public IISSites(ILogService log, IISClient iisClient, IISSiteHelper helper)
        {
            _log = log;
            _iisClient = iisClient;
            _helper = helper;
        }

        public Target Generate(TargetPluginOptions options)
        {
            var myOptions = (IISSitesOptions)options;
            return new Target();
        }
    }
}