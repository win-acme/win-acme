using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISOptions : TargetPluginOptions<IIS>
    {
        /// <summary>
        /// Common name for the certificate
        /// </summary>
        public string? CommonName { get; set; }

        /// <summary>
        /// Search string to select hosts
        /// </summary>
        public string? IncludePattern { get; set; }

        /// <summary>
        /// Regular expression to select hosts
        /// </summary>
        public string? IncludeRegex { get; set; }

        /// <summary>
        /// Filter by hostname
        /// </summary>
        public List<string>? IncludeHosts { get; set; }

        /// <summary>
        /// Excluded bindings (additional filter)
        /// </summary>
        public List<string>? ExcludeHosts { get; set; }

        /// <summary>
        /// Which types of bindings to consider
        /// </summary>
        public List<string>? IncludeTypes { get; set; }

        /// <summary>
        /// Site ids to include in the selection
        /// </summary>
        public List<long>? IncludeSiteIds { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (!string.IsNullOrEmpty(CommonName))
            {
                input.Show("Common name", CommonName, level: 1);
            }

            // Site filter
            if (IncludeSiteIds != null && IncludeSiteIds.Any())
            {
                input.Show("Sites", string.Join(",", IncludeSiteIds), level: 1);
            } 
            else
            {
                input.Show("Sites", "All", level: 1);
            }

            // Binding filter
            if (IncludeRegex != default)
            {
                input.Show("Regex", IncludeRegex.ToString(), level: 1);
            } 
            else if (!string.IsNullOrWhiteSpace(IncludePattern))
            {
                input.Show("Pattern", IncludePattern, level: 1);
            }
            else if (IncludeHosts != null && IncludeHosts.Any())
            {
                input.Show("Hosts", string.Join(',', IncludeHosts), level: 1);
            } 
            else
            {
                input.Show("Hosts", "All", level: 1);
            }

            // Last-minute exclude
            if (ExcludeHosts != null && ExcludeHosts.Any())
            {
                input.Show("Exclude", string.Join(',', ExcludeHosts), level: 1);
            }
        }
    }
}
