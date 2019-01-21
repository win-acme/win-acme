using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("cdd79a68-4a87-4039-bee8-5a0ebdca41cb")]
    class IISSitesOptions : TargetPluginOptions<IISSites>
    {
        public override string Name => "IISSites";
        public override string Description => "SAN certificate for all bindings of multiple IIS sites";

        public bool? All { get; set; }
        public List<long> SiteIds { get; set; }
        public string CommonName { get; set; }
        public List<string> ExcludeBindings { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (All != null)
            {
                input.Show("SiteIds", All == true ? "All" : "None", level: 1);
            }
            if (SiteIds != null)
            {
                input.Show("SiteIds", string.Join(",", SiteIds), level: 1);
            }
            if (!string.IsNullOrEmpty(CommonName))
            {
                input.Show("CommonName", CommonName, level: 1);
            }
            if (ExcludeBindings != null && ExcludeBindings.Count > 0)
            {
                input.Show("ExcludeBindings", string.Join(",", ExcludeBindings), level: 1);
            }
        }
    }
}
