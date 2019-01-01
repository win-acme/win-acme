using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    internal interface IRenewalService
    {
        IEnumerable<Renewal> FindByFriendlyName(string friendlyName);
        void Save(Renewal renewal, RenewResult result);
        void Cancel(Renewal renewal);
        void Clear();
        void Import(Renewal renewal);
        IEnumerable<Renewal> Renewals { get; }
    }
}
