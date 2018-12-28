using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class Manual : ITargetPlugin
    {
        private ILogService _log;

        public Manual(ILogService logService)
        {
            _log = logService;
        }

        Target ITargetPlugin.Generate(TargetPluginOptions options)
        {
            var manualOptions = (ManualOptions)options;
            return new Target()
            {
                CommonName = manualOptions.CommonName,
                Parts = new List<TargetPart> {
                    new TargetPart {
                        Hosts = manualOptions.AlternativeNames
                    }
                }
            };
        }     
    }
}