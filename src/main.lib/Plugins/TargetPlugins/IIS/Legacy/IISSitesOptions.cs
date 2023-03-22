using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSitesOptions : IISOptions
    { 
        public List<long>? SiteIds
        {
            get => null;
            set
            {
                if (IncludeSiteIds == null && value != null)
                {
                    IncludeSiteIds = value;
                }
            }
        }

        public List<string>? ExcludeBindings
        {
            get => null;
            set => ExcludeHosts ??= value;
        }
    }
}
