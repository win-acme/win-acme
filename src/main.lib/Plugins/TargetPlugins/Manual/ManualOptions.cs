using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("e239db3b-b42f-48aa-b64f-46d4f3e9941b")]
    internal class ManualOptions : TargetPluginOptions<Manual>
    {
        public static string DescriptionText = "Manual input";

        public override string Name => "Manual";
        public override string Description => DescriptionText;

        public string? CommonName { get; set; }
        public List<string>? AlternativeNames { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (CommonName != null)
            {
                input.Show("CommonName", CommonName, level: 1);
            }
            if (AlternativeNames != null)
            {
                input.Show("AlternativeNames", string.Join(",", AlternativeNames), level: 1);
            }
        }
    }
}
