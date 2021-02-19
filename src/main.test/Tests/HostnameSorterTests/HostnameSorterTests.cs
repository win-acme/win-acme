using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.HostnameSorterTests
{
    [TestClass]
    public class HostnameSorterTests
    {
        private readonly DomainParseService dp;

        public HostnameSorterTests()
        {
            var log = new Mock.Services.LogService(true);
            var settings = new MockSettingsService();
            var proxy = new ProxyService(log, settings);
            dp = new DomainParseService(log, proxy, settings);
        }

        [TestMethod]
        public void ShortDomainsFirst()
        {
            var input = new[] { "a.example.com", "example.com" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "example.com");
        }

        [TestMethod]
        public void ShortDomainsFirst2()
        {
            var input = new[] { "a.b.example.com", "b.example.com" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "b.example.com");
        }

        [TestMethod]
        public void ShortDomainsFirst3()
        {
            var input = new[] { "b.b.example.com", "a.b.example.com", "b.example.com" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "b.example.com");
            Assert.AreEqual(sorted.Last(), "b.b.example.com");
        }

        [TestMethod]
        public void TldFirst()
        {
            var input = new[] { "zxample.aaa", "example.com" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "example.com");
        }

        [TestMethod]
        public void TldFirst2()
        {
            var input = new[] { "example.com", "example.aaa" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "example.aaa");
        }

        [TestMethod]
        public void RegisterableSecond()
        {
            var input = new[] { "a.example.com", "b.example.com", "example.com" };
            var sorted = input.OrderBy(x => x, new HostnameSorter(dp));
            Assert.AreEqual(sorted.First(), "example.com");
            Assert.AreEqual(sorted.Last(), "b.example.com");
        }
    }
}
