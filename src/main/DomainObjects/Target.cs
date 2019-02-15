using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("Target: {CommonName} ({Parts.Count} part(s) - IIS: {IIS})")]
    public class Target
    {
        /// <summary>
        /// Suggest a FriendlyName for the certificate,
        /// but this may be overruled by the user
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// CommonName for the certificate
        /// </summary>
        public string CommonName { get; set; }

        /// <summary>
        /// Different parts that make up this target
        /// </summary>
        public IEnumerable<TargetPart> Parts { get; set; }

        /// <summary>
        /// Check if all parts are IIS
        /// </summary>
        public bool IIS { get => Parts.All(x => x.IIS); }

        /// <summary>
        /// Pretty print information about the target
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var x = new StringBuilder();
            x.Append(CommonName);
            var alternativeNames = Parts.SelectMany(p => p.Identifiers);
            if (alternativeNames.Count() > 1)
            {
                x.Append($" and {alternativeNames.Count() - 1} alternatives");
            }
            return x.ToString();
        }
    }
}