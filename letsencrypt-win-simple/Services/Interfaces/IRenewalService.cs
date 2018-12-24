using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    internal interface IRenewalService
    {
        ScheduledRenewal Find(Target target);
        void Save(ScheduledRenewal renewal, RenewResult result);
        void Cancel(ScheduledRenewal renewal);
        IEnumerable<ScheduledRenewal> Renewals { get; set; }
    }
}
