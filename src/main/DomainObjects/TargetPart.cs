using System.Collections.Generic;

namespace PKISharp.WACS.DomainObjects
{
    public class TargetPart
    {
        /// <summary>
        /// Optional IIS site ID that sourced these hostnames
        /// </summary>
        public long? SiteId { get; set; }

        /// <summary>
        /// Short check
        /// </summary>
        public bool IIS { get => SiteId != null; }

        /// <summary>
        /// Different parts that make up this target
        /// </summary>
        public IEnumerable<string> Hosts { get; set; }
    }
}
