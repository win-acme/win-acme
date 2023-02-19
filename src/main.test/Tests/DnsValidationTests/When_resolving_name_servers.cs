using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Linq;
using LogService = PKISharp.WACS.UnitTests.Mock.Services.LogService;

namespace PKISharp.WACS.UnitTests.Tests.DnsValidationTests
{
    [TestClass]
    public class When_resolving_name_servers
    {
        private readonly LookupClientProvider _dnsClient;

        public When_resolving_name_servers()
        {
            var log = new LogService(true);
            var settings = new MockSettingsService();
            var proxy = new Mock.Services.ProxyService();
            var domainParser = new DomainParseService(log, proxy, settings);
            _dnsClient = new LookupClientProvider(domainParser, log, settings);
        }

        [TestMethod]
        [DataRow("_acme-challenge.logs.hourstrackercloud.com", "Tx1e8X4LF-c615tnacJeuKmzkRmScZzsU-MJHxdDMhU")]
        //[DataRow("_acme-challenge.www2.candell.org", "IpualE-HBtD8bxr60LoyuLw8FxMPOIUgg2XQTR6mSvw")]
        //[DataRow("_acme-challenge.www2.candell.org", "I2F57jex1qSMXprwPy0crWFSUe2n5AowLitxU0q_WKM")]
        //[DataRow("_acme-challenge.wouter.tinus.online", "DHrsG3LudqI9S0jvitp25tDofK1Jf58J08s3c5rIY3k")]
        //[DataRow("_acme-challenge.www7.candell.org", "xxx")]
        public void Should_recursively_follow_cnames(string challengeUri, string expectedToken)
        {
            //var client = _dnsClient.DefaultClient();
            var auth = _dnsClient.GetAuthority(challengeUri).Result;
            Assert.AreEqual(auth.Domain, "_acme-challenge.logs.hourstrackercloud.com");
            var tokens = auth.Nameservers.First().GetTxtRecords(challengeUri).Result;
            Assert.IsTrue(tokens.Contains(expectedToken));
        }

        [TestMethod]
        [DataRow("activesync.dynu.net")]
        [DataRow("tweakers.net")]
        public void Should_find_nameserver(string domain) => _ = _dnsClient.GetAuthority(domain).Result;


        [TestMethod]
        [DataRow("_acme-challenge.acmedns.wouter.tinus.online")]
        public void Should_Find_Txt(string domain)
        {
            var auth = _dnsClient.GetAuthority(domain).Result;
            //var tokens = auth.Nameservers.First().GetTxtRecords(auth.Domain).Result;
            //Assert.IsTrue(tokens.Any());
            Assert.AreEqual(auth.Domain, "86af4f7c-b82c-4b7d-a75b-3feafbabbb2e.auth.acme-dns.io");
        }
    }
}
