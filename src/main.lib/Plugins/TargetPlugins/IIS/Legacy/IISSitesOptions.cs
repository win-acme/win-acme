using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSitesOptions : IISOptions
    { 
        /// <summary>
        /// Ignored, when this is false the other filter will be
        /// there, and when it's true there is no filter
        /// </summary>
        public bool? All {
            get => null;
            set { }
        }

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
