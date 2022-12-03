using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("2f5dd428-0f5d-4c8a-8fd0-56fc1b5985ce")]
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
