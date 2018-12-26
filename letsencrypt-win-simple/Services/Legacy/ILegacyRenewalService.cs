using System.Collections.Generic;

namespace PKISharp.WACS.Services.Legacy
{
    internal interface ILegacyRenewalService
    {
        IEnumerable<LegacyScheduledRenewal> Renewals { get; }
    }
}
