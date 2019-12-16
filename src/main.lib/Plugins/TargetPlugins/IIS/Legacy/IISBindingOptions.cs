using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("2f5dd428-0f5d-4c8a-8fd0-56fc1b5985ce")]
    internal class IISBindingOptions : IISOptions
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

        /// <summary>
        /// Host name of the binding to look for
        /// </summary>
        public string? Host
        {
            get
            {
                if (IncludeHosts != null)
                {
                    return IncludeHosts.FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    IncludeHosts = new List<string>() { value };
                }
                else
                {
                    IncludeHosts = null;
                }
            }
        }
    }
}
