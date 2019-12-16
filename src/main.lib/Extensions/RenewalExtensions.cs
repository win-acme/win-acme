using PKISharp.WACS.DomainObjects;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class RenewalExtensions
    {
        /// <summary>
        /// Get the most recent thumbprint
        /// </summary>
        /// <returns></returns>
        public static string? Thumbprint(this Renewal renewal)
        {
            return renewal.
                        History?.
                        OrderByDescending(x => x.Date).
                        Where(x => x.Success).
                        Select(x => x.Thumbprint).
                        FirstOrDefault();
        }
    }
}
