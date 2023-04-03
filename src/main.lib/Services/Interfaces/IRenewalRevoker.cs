using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface IRenewalRevoker
    {
        Task RevokeCertificates(IEnumerable<Renewal> renewals);
        Task CancelRenewals(IEnumerable<Renewal> renewals);
    }
}
