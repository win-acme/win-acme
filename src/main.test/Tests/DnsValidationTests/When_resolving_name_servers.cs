using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nager.PublicSuffix;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
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
			var domainParser = new DomainParseService();
			var log = new LogService(true);
            _dnsClient = new LookupClientProvider(domainParser, log);
		}

		[TestMethod]
		[DataRow("_acme-challenge.logs.hourstrackercloud.com", "Tx1e8X4LF-c615tnacJeuKmzkRmScZzsU-MJHxdDMhU")]
		//[DataRow("_acme-challenge.www2.candell.org", "IpualE-HBtD8bxr60LoyuLw8FxMPOIUgg2XQTR6mSvw")]
		//[DataRow("_acme-challenge.www2.candell.org", "I2F57jex1qSMXprwPy0crWFSUe2n5AowLitxU0q_WKM")]
        [DataRow("_acme-challenge.wouter.tinus.online", "DHrsG3LudqI9S0jvitp25tDofK1Jf58J08s3c5rIY3k")]
        //[DataRow("_acme-challenge.www7.candell.org", "xxx")]
        public void Should_recursively_follow_cnames(string challengeUri, string expectedToken)
		{
            //var client = _dnsClient.DefaultClient();
            var client = _dnsClient.GetClient(challengeUri);
            var tokens = client.GetTextRecordValues(challengeUri, 0);
			Assert.IsTrue(tokens.Contains(expectedToken));
		}
	}
}
