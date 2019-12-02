using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISSiteTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISBindingHelper helper;
        private readonly IPluginService plugins;
        private readonly UserRoleService userRoleService;

        public IISSiteTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            helper = new IISBindingHelper(log, iis);
            plugins = new MockPluginService(log);
            userRoleService = new UserRoleService(iis);
        }

        private IISBindingsOptions Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            var x = new IISBindingsOptionsFactory(log, iis, helper, arguments, userRoleService);
            return x.Default().Result;
        }

        private Target Target(IISBindingsOptions options)
        {
            var plugin = new IISBindings(log, userRoleService, helper, options);
            return plugin.Generate().Result;
        }

        [TestMethod]
        public void Regular()
        {
            var siteId = 1;
            var site = iis.GetWebSite(siteId);
            var options = Options($"--siteid {siteId}");
            Assert.IsNotNull(options);
            Assert.IsNotNull(options.IncludeSiteIds);
            Assert.AreEqual(options.IncludeSiteIds.Count(), 1);
            Assert.IsTrue(options.IncludeSiteIds.Contains(1));
            Assert.IsNull(options.CommonName);
            Assert.IsNull(options.ExcludeHosts);
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
            _ = iis.GetWebSite(siteId);
            var options = Options($"--siteid {siteId} --commonname {commonName}");
            Assert.AreEqual(options.IncludeSiteIds.FirstOrDefault(), siteId);
            Assert.AreEqual(options.CommonName, commonName);
            Assert.IsNull(options.ExcludeHosts);
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
            Assert.AreEqual(options.IncludeSiteIds.FirstOrDefault(), siteId);
            Assert.AreEqual(options.CommonName, uniHost);
            Assert.IsNull(options.ExcludeHosts);
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
            Assert.AreEqual(options.IncludeSiteIds.FirstOrDefault(), siteId);
            Assert.IsNotNull(options.ExcludeHosts);
            Assert.AreEqual(options.ExcludeHosts.Count(), site.Bindings.Count() - 2);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, "alt.example.com"); // 2nd binding, first is excluded
        }

        [TestMethod]
        public void ExcludeBindingsPuny()
        {
            var siteId = 1;
            var options = Options($"--siteid {siteId} --excludebindings xn--/-9b3b774gbbb.example.com");
            Assert.AreEqual(options.IncludeSiteIds.FirstOrDefault(), siteId);
            Assert.IsNotNull(options.ExcludeHosts);
            Assert.AreEqual(options.ExcludeHosts.Count(), 1);
            Assert.AreEqual(options.ExcludeHosts.First(), "经/已經.example.com");
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
        public void NoSite() => Options($"");

        [TestMethod]
        public void IllegalSite()
        {
            var options = Options($"--siteid ab");
            Assert.IsNull(options);
        }
    }
}