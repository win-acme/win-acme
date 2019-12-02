using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("cdd79a68-4a87-4039-bee8-5a0ebdca41cb")]
    internal class IISSitesOptions : IISBindingsOptions
    { 
        public bool? All { get; set; }
        public List<long>? SiteIds { get; set; }
        public List<string>? ExcludeBindings { get; set; }
    }
}
