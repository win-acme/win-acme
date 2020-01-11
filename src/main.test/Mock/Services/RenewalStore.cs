using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockRenewalStore : IRenewalStore
    {
        public IEnumerable<Renewal> Renewals => throw new System.NotImplementedException();
        public void Cancel(Renewal renewal) => throw new System.NotImplementedException();
        public void Clear() => throw new System.NotImplementedException();
        public void Encrypt() => throw new System.NotImplementedException();
        public IEnumerable<Renewal> FindByArguments(string id, string friendlyName) => throw new System.NotImplementedException();
        public void Import(Renewal renewal) => throw new System.NotImplementedException();
        public void Save(Renewal renewal, RenewResult result) => throw new System.NotImplementedException();
    }
}
