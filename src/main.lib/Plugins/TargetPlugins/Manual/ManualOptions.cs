using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualOptions : TargetPluginOptions
    {
        public const string DescriptionText = "Manual input";
        public string? CommonName { get; set; }
        public List<string> AlternativeNames { get; set; } = new List<string>();

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
