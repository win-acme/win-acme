using PKISharp.WACS.DomainObjects.Legacy;
using System.Collections.Generic;

namespace PKISharp.WACS.Services.Renewal.Legacy
{
    internal interface ILegacyRenewalService
    {
        IEnumerable<ScheduledRenewal> Renewals { get; }
    }
}
