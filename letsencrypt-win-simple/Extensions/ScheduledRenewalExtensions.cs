using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class ScheduledRenewalExtensions
    {
        /// <summary>
        /// Get the most recent thumbprint
        /// </summary>
        /// <returns></returns>
        public static string Thumbprint(this ScheduledRenewal renewal)
        {
            return renewal.
                        History?.
                        OrderByDescending(x => x.Date).
                        Where(x => x.Success).
                        Select(x => x.Thumbprint).
                        FirstOrDefault();
        }

        /// <summary>
        /// Find the most recently issued certificate for a specific target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static CertificateInfo Certificate(this ScheduledRenewal renewal, IStorePlugin store)
        {
            var thumbprint = renewal.Thumbprint();
            var useThumbprint = !string.IsNullOrEmpty(thumbprint);
            if (useThumbprint)
            {
                return store.FindByThumbprint(thumbprint);
            }
            else
            {
                return null;
            }
        }
    }
}
