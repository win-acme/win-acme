using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISBindingTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISHelper helper;
        private readonly MockPluginService plugins;
        private readonly IUserRoleService userRoleService;
        private readonly DomainParseService domainParse;
        public IISBindingTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            var settings = new MockSettingsService();
            var proxy = new Mock.Services.ProxyService();
            domainParse = new DomainParseService(log, proxy, settings);
            helper = new IISHelper(log, iis, domainParse);
            plugins = new MockPluginService(log);
            userRoleService = new Mock.Services.UserRoleService();
        }

        private IISOptions? Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var input = new Mock.Services.InputService(new());
            var secretService = new SecretServiceManager(new SecretService(), input, log);
            var argsInput = new ArgumentsInputService(log, optionsParser, input, secretService);
            var args = new MainArguments();
            var x = new IISOptionsFactory(log, helper, args, argsInput, userRoleService);
            return x.Default().Result;
        }

        private Target Target(IISOptions options)
        {
            var plugin = new IIS(log, userRoleService, helper, options);
            return plugin.Generate().Result;
        }

        private void TestHost(string host, long siteId)
        {
            var result = Options($"--siteid {siteId} --host {host}");
            Assert.IsNotNull(result);
            if (result != null)
            {
                Assert.AreEqual(result.IncludeSiteIds?.FirstOrDefault(), siteId);
                Assert.AreEqual(result.IncludeHosts?.FirstOrDefault(), host);

                var target = Target(result);
                Assert.IsNotNull(target);
                Assert.AreEqual(target.IsValid(log), true);
                Assert.AreEqual(target.CommonName.Value, host);
                Assert.AreEqual(target.Parts.Count(), 1);
                Assert.AreEqual(target.Parts.First().SiteId, siteId);
                Assert.AreEqual(target.Parts.First().Identifiers.Count(), 1);
                Assert.AreEqual(target.Parts.First().Identifiers.First().Value, host);
            }
           
        }

        [TestMethod]
        public void Regular() => TestHost("test.example.com", 1);

        [TestMethod]
        public void IDNUnicode() => TestHost("经/已經.example.com", 1);

        [TestMethod]
        public void IDNAscii()
        {
            var punyHost = "xn--/-9b3b774gbbb.example.com";
            var uniHost = "经/已經.example.com";
            var siteId = 1;
            var result = Options($"--siteid {siteId} --host {punyHost}");
            Assert.IsNotNull(result);
            if (result != null)
            {
                Assert.AreEqual(result.IncludeSiteIds?.FirstOrDefault(), siteId);
                Assert.AreEqual(result.IncludeHosts?.FirstOrDefault(), uniHost);

                var target = Target(result);
                Assert.IsNotNull(target);
                Assert.AreEqual(target.IsValid(log), true);
                Assert.AreEqual(target.IIS, true);
                Assert.AreEqual(target.CommonName.Value, uniHost);
                Assert.AreEqual(target.Parts.Count(), 1);
                Assert.AreEqual(target.Parts.First().SiteId, siteId);
                Assert.AreEqual(target.Parts.First().Identifiers.Count(), 1);
                Assert.AreEqual(target.Parts.First().Identifiers.First().Value, uniHost);
            }
        }

        [TestMethod]
        public void WrongSite()
        {
            var result = Options("--siteid 2 --host test.example.com");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void MissingSite()
        {
            var result = Options("--siteid 999 --host test.example.com");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void IllegalSite()
        {
            var result = Options("--siteid ab --host test.example.com");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void WrongHost()
        {
            var result = Options("--siteid 1 --host doesntexist.example.com");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void BindingMissing()
        {
            var options = new IISBindingOptions() { Host = "doesntexist.example.com", SiteId = 1 };
            var target = Target(options);
            Assert.IsTrue(target is INull);
        }
    }
}
