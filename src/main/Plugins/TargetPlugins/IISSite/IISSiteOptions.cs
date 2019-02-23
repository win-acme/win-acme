using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("d7940b23-f570-460e-ab15-2c822a79009b")]
    class IISSiteOptions : TargetPluginOptions<IISSite>, IIISSiteOptions
    {
        public override string Name => "IISSite";
        public override string Description => "SAN certificate for all bindings of an IIS site";

        public long SiteId { get; set; }
        public string CommonName { get; set; }
        public List<string> ExcludeBindings { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("SiteId", SiteId.ToString(), level: 1);
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
