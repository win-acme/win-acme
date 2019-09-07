using System.Collections.Generic;
using System.Diagnostics;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("TargetPart: ({Identifiers.Count} host(s) - IIS: {IIS})")]
    public class TargetPart
    {
        /// <summary>
        /// Optional IIS site ID that sourced these hostnames
        /// </summary>
        public long? SiteId { get; set; }

        /// <summary>
        /// Short check
        /// </summary>
        public bool IIS => SiteId != null;

        /// <summary>
        /// <summary>
        /// Different parts that make up this target
        /// </summary>
        public List<string> Identifiers { get; set; }
    }
}
