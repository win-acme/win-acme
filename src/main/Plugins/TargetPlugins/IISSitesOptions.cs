using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class IISSitesOptions : TargetPluginOptions<IISSites>
    {
        public override string Name => "IISSites";
        public override string Description => "SAN certificate for all bindings of multiple IIS sites";

        public bool? All { get; set; }
        public List<long> SiteIds { get; set; }
        public string CommonName { get; set; }
        public List<string> ExcludeBindings { get; set; }
    }
}
