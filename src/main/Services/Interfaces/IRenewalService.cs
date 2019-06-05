using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    internal interface IRenewalService
    {
        IEnumerable<Renewal> FindByArguments(string id, string friendlyName);
        void Save(Renewal renewal, RenewResult result);
        void Cancel(Renewal renewal);
        void Clear();
        void Import(Renewal renewal);
        void Export();
        IEnumerable<Renewal> Renewals { get; }
    }
}
