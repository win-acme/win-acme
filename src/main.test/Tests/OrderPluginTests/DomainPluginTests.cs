using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.OrderPlugins;
using PKISharp.WACS.UnitTests.Mock;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.OrderPluginTests
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

        [TestMethod]
        public void DomainSplitAdvanced()
        {
            var x_com = new DnsIdentifier("x.com");
            var www_x_com = new DnsIdentifier("www.x.com");
            var ftp_x_com = new DnsIdentifier("ftp.x.com");

            var y_com = new DnsIdentifier("y.com");
            var www_y_com = new DnsIdentifier("www.y.com");
            var ftp_y_com = new DnsIdentifier("ftp.y.com");

            var parts = new TargetPart[] {
                new TargetPart(new[] { x_com,www_x_com }) { SiteId = 1, SiteType = Clients.IIS.IISSiteType.Web },
                new TargetPart(new[] { y_com,www_y_com }) { SiteId = 2, SiteType = Clients.IIS.IISSiteType.Web },
                new TargetPart(new[] { ftp_x_com, ftp_y_com }) { SiteId = 3, SiteType = Clients.IIS.IISSiteType.Ftp }
            };
            var target = new Target("x.com", www_y_com, parts);
            var renewal = new Renewal();
            var container = new MockContainer().TestScope();
            var domain = container.Resolve<Domain>();
            var split = domain.Split(renewal, target);
            Assert.IsNotNull(split);
            var list = split.ToList();
            Assert.AreEqual(2, list.Count);

            // First order for X.com, two parts for sites 1 and 3
            Assert.AreEqual(x_com, list[0].Target.CommonName);
            var prts = list[0].Target.Parts.ToList();
            Assert.AreEqual(2, prts.Count);

            var prt = prts[0];
            Assert.AreEqual(1, prt.SiteId);
            Assert.AreEqual(Clients.IIS.IISSiteType.Web, prt.SiteType);
            Assert.AreEqual(2, prt.Identifiers.Count);

            var ids = prt.Identifiers;
            Assert.IsTrue(ids.Contains(x_com));
            Assert.IsTrue(ids.Contains(www_x_com));

            prt = prts[1];
            Assert.AreEqual(3, prt.SiteId);
            Assert.AreEqual(Clients.IIS.IISSiteType.Ftp, prt.SiteType);
            Assert.AreEqual(1, prt.Identifiers.Count);

            ids = prt.Identifiers;
            Assert.IsTrue(ids.Contains(ftp_x_com));

            // Second order for X.com, two parts for sites 2 and 3
            Assert.AreEqual(y_com, list[1].Target.CommonName);
            prts = list[1].Target.Parts.ToList();
            Assert.AreEqual(2, prts.Count);

            prt = prts[0];
            Assert.AreEqual(2, prt.SiteId);
            Assert.AreEqual(Clients.IIS.IISSiteType.Web, prt.SiteType);
            Assert.AreEqual(2, prt.Identifiers.Count);

            ids = prt.Identifiers;
            Assert.IsTrue(ids.Contains(y_com));
            Assert.IsTrue(ids.Contains(www_y_com));

            prt = prts[1];
            Assert.AreEqual(3, prt.SiteId);
            Assert.AreEqual(Clients.IIS.IISSiteType.Ftp, prt.SiteType);
            Assert.AreEqual(1, prt.Identifiers.Count);

            ids = prt.Identifiers;
            Assert.IsTrue(ids.Contains(ftp_y_com));
        }
    }
}
