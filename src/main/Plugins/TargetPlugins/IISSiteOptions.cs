using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class IISSiteOptions : TargetPluginOptions<IISSite>
    {
        public override string Name => "IISSite";
        public override string Description => "SAN certificate for all bindings of an IIS site";

        public long SiteId { get; set; }
        public string CommonName { get; set; }
        public List<string> ExcludeBindings { get; set; }
    }
}
