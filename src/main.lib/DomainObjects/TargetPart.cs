using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("TargetPart: ({Identifiers.Count} host(s) - IIS: {IIS})")]
    public class TargetPart
    {
        public TargetPart(IEnumerable<Identifier>? identifiers)
        {
            if (identifiers == null)
            {
                throw new ArgumentNullException(nameof(identifiers));
            }
            Identifiers = identifiers.ToList();
        }
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
        public List<Identifier> Identifiers { get; }
    }
}
