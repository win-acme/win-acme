using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class Manual : ITargetPlugin
    {
        private ILogService _log;
        private ManualOptions _options;

        public Manual(ILogService logService, ManualOptions options)
        {
            _log = logService;
            _options = options;
        }

        public Target Generate()
        {
            return new Target()
            {
                CommonName = _options.CommonName,
                Parts = new List<TargetPart> {
                    new TargetPart {
                        Identifiers = _options.AlternativeNames
                    }
                }
            };
        }     
    }
}