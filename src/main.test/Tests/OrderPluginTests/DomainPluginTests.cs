using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.OrderPlugins;
using PKISharp.WACS.UnitTests.Mock;

namespace PKISharp.WACS.UnitTests.Tests.EcnryptionTests
{
    [TestClass]
    public class DomainPluginTests
    {
        [TestMethod]
        public void DomainSplit()
        {
            var parts = new TargetPart[] { new TargetPart(new[] { new DnsIdentifier("x.com") }) };
            var target = new Target("x.com", "x.com", parts);
            var renewal = new Renewal();
            var container = new MockContainer().TestScope();
            var domain = container.Resolve<Domain>();
            var split = domain.Split(renewal, target);
            Assert.IsNotNull(split);
        }
    }
}
