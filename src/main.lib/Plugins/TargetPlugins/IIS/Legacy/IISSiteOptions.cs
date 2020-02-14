using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("d7940b23-f570-460e-ab15-2c822a79009b")]
    internal class IISSiteOptions : IISOptions
    {
        public long? SiteId
        {
            get => null;
            set
            {
                if (IncludeSiteIds == null && value.HasValue)
                {
                    IncludeSiteIds = new List<long>() { value.Value };
                }
            }
        }

        public List<string>? ExcludeBindings {
            get => null;
            set
            {
                if (ExcludeHosts == null)
                {
                    ExcludeHosts = value;
                }
            }
        }
    }
}
