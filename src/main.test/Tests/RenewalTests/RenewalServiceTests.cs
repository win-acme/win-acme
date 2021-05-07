using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using real = PKISharp.WACS.Services;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using System.Linq;
using System.Collections.Generic;
using PKISharp.WACS.UnitTests.Mock;

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
            var renewalStore = container.Resolve<real.IRenewalStore>();
            var renewalValidator = container.Resolve<RenewalValidator>(
                new TypedParameter(typeof(IContainer), container));
            var renewalExecutor = container.Resolve<RenewalExecutor>(
               new TypedParameter(typeof(RenewalValidator), renewalValidator),
               new TypedParameter(typeof(IContainer), container));
            var renewalManager = container.Resolve<RenewalManager>(
                new TypedParameter(typeof(IContainer), container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
            Assert.IsNotNull(renewalManager);
            renewalManager.ManageRenewals().Wait();
            Assert.AreEqual(0, renewalStore.Renewals.Count());
        }

    }
}
