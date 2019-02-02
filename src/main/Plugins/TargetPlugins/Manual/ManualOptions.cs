using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("e239db3b-b42f-48aa-b64f-46d4f3e9941b")]
    class ManualOptions : TargetPluginOptions<Manual>
    {
        public static string DescriptionText = "Manually input host names";

        public override string Name => "Manual";
        public override string Description => DescriptionText;

        public string CommonName { get; set; }
        public List<string> AlternativeNames { get; set; }
    }
}
