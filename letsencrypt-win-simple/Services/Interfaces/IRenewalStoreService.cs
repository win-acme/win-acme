using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Services.RenewalStore
{
    interface IRenewalStoreService
    {
        IEnumerable<ScheduledRenewal> Renewals { get; set; }
    }
}
