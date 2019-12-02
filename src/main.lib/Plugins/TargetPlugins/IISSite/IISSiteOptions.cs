using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("d7940b23-f570-460e-ab15-2c822a79009b")]
    internal class IISSiteOptions : IISBindingsOptions
    {
        public long? SiteId
        {
            get
            {
                if (IncludeSiteIds != null)
                {
                    return IncludeSiteIds.FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value.HasValue)
                {
                    IncludeSiteIds = new List<long>() { value.Value };
                }
                else
                {
                    IncludeSiteIds = null;
                }
            }
        }

        public List<string>? ExcludeBindings { 
            get => ExcludeHosts;
            set => ExcludeHosts = value;
        }
    }
}
