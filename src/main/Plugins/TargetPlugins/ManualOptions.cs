using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class ManualOptions : TargetPluginOptions<Manual>
    {
        public override string Name => "Manual";
        public override string Description => "Manually input host names";

        public string CommonName { get; set; }
        public List<string> AlternativeNames { get; set; }
    }
}
