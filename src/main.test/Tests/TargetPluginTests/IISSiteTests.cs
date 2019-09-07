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
    public class IISSiteTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISSiteHelper helper;
        private readonly PluginService plugins;

        public IISSiteTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            helper = new IISSiteHelper(log, iis);
            plugins = new PluginService(log);
        }

        private IISSiteOptions Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            var x = new IISSiteOptionsFactory(log, iis, helper, arguments);
            return x.Default();
        }

        private Target Target(IISSiteOptions options)
        {
            var plugin = new IISSite(log, helper, options);
            return plugin.Generate();
        }

        [TestMethod]
        public void Regular()
        {
            var siteId = 1;
            var site = iis.GetWebSite(siteId);
            var options = Options($"--siteid {siteId}");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.SiteId, siteId);
            Assert.IsNull(options.CommonName);
            Assert.IsNull(options.ExcludeBindings);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, site.Bindings.First().Host); // First binding
            Assert.AreEqual(target.IIS, true);
            Assert.AreEqual(target.Parts.Count(), 1);
            Assert.AreEqual(target.Parts.First().SiteId, siteId);
            Assert.AreEqual(target.Parts.First().Identifiers.Count(), site.Bindings.Count());
            Assert.AreEqual(target.Parts.First().Identifiers.All(x => site.Bindings.Any(b => b.Host == x)), true);
        }

        [TestMethod]
        public void CommonNameUni()
        {
            var siteId = 1;
            var commonName = "经/已經.example.com";
            var site = iis.GetWebSite(siteId);
            var options = Options($"--siteid {siteId} --commonname {commonName}");
            Assert.AreEqual(options.SiteId, siteId);
            Assert.AreEqual(options.CommonName, commonName);
            Assert.IsNull(options.ExcludeBindings);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, commonName); // First binding
        }

        [TestMethod]
        public void CommonNamePuny()
        {
            var siteId = 1;
            var punyHost = "xn--/-9b3b774gbbb.example.com";
            var uniHost = "经/已經.example.com";
            var options = Options($"--siteid {siteId} --commonname {punyHost}");
            Assert.AreEqual(options.SiteId, siteId);
            Assert.AreEqual(options.CommonName, uniHost);
            Assert.IsNull(options.ExcludeBindings);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, uniHost); // First binding
        }

        [TestMethod]
        public void ExcludeBindings()
        {
            var siteId = 1;
            var site = iis.GetWebSite(siteId);
            var options = Options($"--siteid {siteId} --excludebindings test.example.com,four.example.com");
            Assert.AreEqual(options.SiteId, siteId);
            Assert.IsNotNull(options.ExcludeBindings);
            Assert.AreEqual(options.ExcludeBindings.Count(), site.Bindings.Count() - 2);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, "alt.example.com"); // 2nd binding, first is excluded
        }

        [TestMethod]
        public void ExcludeBindingsPuny()
        {
            var siteId = 1;
            var options = Options($"--siteid {siteId} --excludebindings xn--/-9b3b774gbbb.example.com");
            Assert.AreEqual(options.SiteId, siteId);
            Assert.IsNotNull(options.ExcludeBindings);
            Assert.AreEqual(options.ExcludeBindings.Count(), 1);
            Assert.AreEqual(options.ExcludeBindings.First(), "经/已經.example.com");
        }

        [TestMethod]
        public void CommonNameExcluded()
        {
            var siteId = 1;
            var options = Options($"--siteid {siteId} --commonname test.example.com --excludebindings test.example.com,four.example.com");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void CommonNameExcludedAfter()
        {
            var siteId = 1;
            var site = iis.GetWebSite(siteId);
            var options = new IISSiteOptions() { SiteId = siteId, CommonName = "missing.example.com" };
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, site.Bindings.First().Host);
        }

        [TestMethod]
        public void MissingSiteConfig()
        {
            var options = Options($"--siteid 999");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void MissingSiteExecution()
        {
            var options = new IISSiteOptions()
            {
                SiteId = 999
            };
            var target = Target(options);
            Assert.IsNull(target);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void NoSite()
        {
            var options = Options($"");
        }

        [TestMethod]
        public void IllegalSite()
        {
            var options = Options($"--siteid ab");
            Assert.IsNull(options);
        }
    }
}