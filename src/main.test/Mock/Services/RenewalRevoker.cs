using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockRenewalRevoker : IRenewalRevoker
    {
        private readonly IRenewalStore _store;

        public MockRenewalRevoker(IRenewalStore renewalStore) 
        {
            _store = renewalStore;
        }

        public Task CancelRenewals(IEnumerable<Renewal> renewals)
        {
            foreach (var renewal in renewals) {
                _store.Cancel(renewal);
            }
            return Task.CompletedTask;
        }

        public Task RevokeCertificates(IEnumerable<Renewal> renewals)
        {
            throw new System.NotImplementedException();
        }
    }
}
