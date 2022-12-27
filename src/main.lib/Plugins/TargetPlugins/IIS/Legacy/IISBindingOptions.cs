using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingOptions : IISOptions
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

        /// <summary>
        /// Host name of the binding to look for
        /// </summary>
        public string? Host
        {
            get => null;
            set
            {
                if (IncludeHosts == null && !string.IsNullOrEmpty(value))
                {
                    IncludeHosts = new List<string>() { value };
                }
            }
        }
    }
}
