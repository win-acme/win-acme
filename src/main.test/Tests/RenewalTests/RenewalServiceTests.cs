using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.UnitTests.Mock;
using System.Collections.Generic;
using System.Linq;
using Real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Tests.RenewalTests
{
    [TestClass]
    public class RenewalManagerTests
    {
        [TestMethod]
        public void Simple()
        {
            var container = new MockContainer().TestScope(new List<string>()
            {
                "C", // Cancel command
                "y", // Confirm cancel all
                "Q" // Quit
            });
            var renewalStore = container.Resolve<Real.IRenewalStore>();
            var renewalManager = container.Resolve<RenewalManager>();
          
            Assert.IsNotNull(renewalManager);
            renewalManager.ManageRenewals().Wait();
            Assert.AreEqual(0, renewalStore.Renewals.Count());
        }

    }
}
