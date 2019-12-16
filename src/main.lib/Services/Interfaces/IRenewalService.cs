using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    internal interface IRenewalStore
    {
        IEnumerable<Renewal> FindByArguments(string? id, string? friendlyName);
        void Save(Renewal renewal, RenewResult result);
        void Cancel(Renewal renewal);
        void Clear();
        void Import(Renewal renewal);
        void Encrypt();
        IEnumerable<Renewal> Renewals { get; }
    }
}
