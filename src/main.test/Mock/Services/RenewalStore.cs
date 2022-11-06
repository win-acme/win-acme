using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockRenewalStore : RenewalStore
    {
        /// <summary>
        /// Local cache to prevent superfluous reading and
        /// JSON parsing
        /// </summary>
        internal List<Renewal> _renewalsCache;

        public MockRenewalStore(
          ISettingsService settings, 
          ILogService log,
          IInputService input, 
          PasswordGenerator password,
          IDueDateService dueDate,
          IPluginService plugin, 
          ICacheService certificateService) :
          base(settings, log, input, password, plugin, dueDate, certificateService)
        {
            _renewalsCache = new List<Renewal>
            {
                new Renewal() { Id = "1" }
            };
        }

        protected override IEnumerable<Renewal> ReadRenewals() => _renewalsCache;
        protected override void WriteRenewals(IEnumerable<Renewal> Renewals) => _renewalsCache = Renewals.Where(x => !x.Deleted).ToList();
    }
}
