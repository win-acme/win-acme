using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockRenewalStore : IRenewalStoreBackend
    {
        /// <summary>
        /// Local cache to prevent superfluous reading and
        /// JSON parsing
        /// </summary>
        internal List<Renewal> _renewalsCache;

        public MockRenewalStore()
        {
            _renewalsCache = new List<Renewal>
            {
                new Renewal() { Id = "1" }
            };
        }

        public IEnumerable<Renewal> Read()
        {
            return _renewalsCache.Where(x => !x.Deleted).ToList();
        }

        public void Write(IEnumerable<Renewal> renewals)
        {
            _renewalsCache = renewals.ToList();
        }
    }
}
