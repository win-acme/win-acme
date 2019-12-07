using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("cdd79a68-4a87-4039-bee8-5a0ebdca41cb")]
    internal class IISSitesOptions : IISBindingsOptions
    { 
        /// <summary>
        /// Ignored, when this is false the other filter will be
        /// there, and when it's true there is no filter
        /// </summary>
        public bool? All { get; set; }
        public List<long>? SiteIds
        {
            get => IncludeSiteIds;
            set => IncludeSiteIds = value;
        }
        public List<string>? ExcludeBindings
        {
            get => ExcludeHosts;
            set => ExcludeHosts = value;
        }
    }
}
