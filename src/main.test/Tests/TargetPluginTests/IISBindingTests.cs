using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISBindingTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISBindingHelper helper;
        private readonly PluginService plugins;

        public IISBindingTests()
        {
            log = new Mock.Services.LogService();
            iis = new Mock.Clients.MockIISClient();
            helper = new IISBindingHelper(log, iis);
            plugins = new PluginService(log);
        }

        private IISBindingOptions Options(string commandLine)
        {
            var x = new IISBindingOptionsFactory(log, iis, helper);
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            return x.Default(arguments);
        }

        private Target Target(IISBindingOptions options)
        {
            var plugin = new IISBinding(log, iis, helper, options);
            return plugin.Generate();
        }

        private void TestHost(string host, long siteId)
        {
            var result = Options($"--siteid {siteId} --host {host}");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.SiteId, siteId);
            Assert.AreEqual(result.Host, host);

            var target = Target(result);
            Assert.IsNotNull(target);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, host);
            Assert.AreEqual(target.Parts.Count(), 1);
            Assert.AreEqual(target.Parts.First().SiteId, siteId);
            Assert.AreEqual(target.Parts.First().Identifiers.Count(), 1);
            Assert.AreEqual(target.Parts.First().Identifiers.First(), host);
        }

        [TestMethod]
        public void Regular()
        {
            TestHost("test.example.com", 1);
        }

        [TestMethod]
        public void IDNUnicode()
        {
            TestHost("经/已經.example.com", 1);
        }

        [TestMethod]
        public void IDNAscii()
        {
            var punyHost = "xn--/-9b3b774gbbb.example.com";
            var uniHost = "经/已經.example.com";
            var siteId = 1;
            var result = Options($"--siteid {siteId} --host {punyHost}");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.SiteId, siteId);
            Assert.AreEqual(result.Host, uniHost);

            var target = Target(result);
            Assert.IsNotNull(target);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.IIS, true);
            Assert.AreEqual(target.CommonName, uniHost);
            Assert.AreEqual(target.Parts.Count(), 1);
            Assert.AreEqual(target.Parts.First().SiteId, siteId);
            Assert.AreEqual(target.Parts.First().Identifiers.Count(), 1);
            Assert.AreEqual(target.Parts.First().Identifiers.First(), uniHost);
        }

        [TestMethod]
        public void NoSite()
        {
            var result = Options("--host test.example.com");
            Assert.IsNotNull(result);
            Assert.AreEqual(result.SiteId, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void NoHost()
        {
            var result = Options("--siteid 1");
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
            Assert.IsNull(target);
        }

        [TestMethod]
        public void BindingMoved()
        {
            var options = new IISBindingOptions() { Host = "test.example.com", SiteId = 2 };
            var target = Target(options);
            Assert.AreEqual(target.Parts.First().SiteId, 1);
        }
    }
}
