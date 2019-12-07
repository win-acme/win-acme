using PKISharp.WACS.Plugins.Base;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("2f5dd428-0f5d-4c8a-8fd0-56fc1b5985ce")]
    internal class IISBindingOptions : IISBindingsOptions
    {
        /// <summary>
        /// Restrict search to a specific site
        /// Old system didn't filter by site, so also allow the
        /// backwards compatibility to work that way. Basically
        /// just ignore this setting.
        /// </summary>
        public long? SiteId { get; set; }

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
