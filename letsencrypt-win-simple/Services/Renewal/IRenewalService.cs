using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Services.Renewal
{
    interface IRenewalService
    {
        ScheduledRenewal Find(Target target);
        void Save(ScheduledRenewal renewal, RenewResult result);
        void Cancel(ScheduledRenewal renewal);
        IEnumerable<ScheduledRenewal> Renewals { get; set; }
    }
}
