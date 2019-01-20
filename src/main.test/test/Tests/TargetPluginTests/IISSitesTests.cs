using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISSitesTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISSiteHelper helper;
        private readonly PluginService plugins;

        public IISSitesTests()
        {
            log = new Mock.Services.LogService();
            iis = new Mock.Clients.MockIISClient();
            helper = new IISSiteHelper(log, iis);
            plugins = new PluginService(log);
        }

        private IISSitesOptions Options(string commandLine)
        {
            var x = new IISSitesOptionsFactory(log, iis, helper);
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var optionsService = new OptionsService(log, optionsParser);
            return x.Default(optionsService);
        }

        private Target Target(IISSitesOptions options)
        {
            var plugin = new IISSites(log, iis, helper, options);
            return plugin.Generate();
        }

        [TestMethod]
        public void Regular()
        {
            var siteIdA = 1;
            var siteIdB = 2;
            var siteA = iis.GetWebSite(siteIdA);
            var siteB = iis.GetWebSite(siteIdB);
            var options = Options($"--siteid {siteIdA},{siteIdB}");
            Assert.IsNotNull(options);
            Assert.IsNotNull(options.SiteIds);
            Assert.AreEqual(options.SiteIds.Contains(siteIdA), true);
            Assert.AreEqual(options.SiteIds.Contains(siteIdB), true);
            Assert.IsNull(options.CommonName);
            Assert.IsNull(options.ExcludeBindings);
            Assert.IsNull(options.All);

            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, siteA.Bindings.First().Host); // First binding
            Assert.AreEqual(target.IIS, true);
            Assert.AreEqual(target.Parts.Count(), 2);
            Assert.AreEqual(target.Parts.First().SiteId, siteIdA);
            Assert.AreEqual(target.Parts.First().Identifiers.Count(), siteA.Bindings.Count());
            Assert.AreEqual(target.Parts.First().Identifiers.All(x => siteA.Bindings.Any(b => b.Host == x)), true);

            Assert.AreEqual(target.Parts.Last().SiteId, siteIdB);
            Assert.AreEqual(target.Parts.Last().Identifiers.Count(), siteB.Bindings.Count());
            Assert.AreEqual(target.Parts.Last().Identifiers.All(x => siteB.Bindings.Any(b => b.Host == x)), true);
        }

        [TestMethod]
        public void CommonNameUni()
        {
            var commonName = "经/已經.example.com";
            var options = Options($"--siteid 1,2 --commonname {commonName}");
            Assert.AreEqual(options.CommonName, commonName);
            Assert.IsNull(options.ExcludeBindings);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, commonName);
        }

        [TestMethod]
        public void CommonNamePuny()
        {
            var punyHost = "xn--/-9b3b774gbbb.example.com";
            var uniHost = "经/已經.example.com";
            var options = Options($"--siteid 1,2 --commonname {punyHost}");
            Assert.AreEqual(options.CommonName, uniHost);
            Assert.IsNull(options.ExcludeBindings);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, uniHost);
        }

        [TestMethod]
        public void ExcludeBindings()
        {
            var siteIdA = 1;
            var siteIdB = 2;
            var siteA = iis.GetWebSite(siteIdA);
            var siteB = iis.GetWebSite(siteIdB);
            var options = Options($"--siteid 1,2 --excludebindings {siteA.Bindings.ElementAt(0).Host},four.example.com");
            Assert.IsNotNull(options.ExcludeBindings);
            Assert.AreEqual(options.ExcludeBindings.Count(), 2);
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, siteA.Bindings.ElementAt(1).Host); // 2nd binding, first is excluded
        }

        [TestMethod]
        public void ExcludeBindingsPuny()
        {
            var siteIdA = 1;
            var siteIdB = 2;
            var siteA = iis.GetWebSite(siteIdA);
            var siteB = iis.GetWebSite(siteIdB);
            var options = Options($"--siteid 1,2 --excludebindings xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options.ExcludeBindings);
            Assert.AreEqual(options.ExcludeBindings.Count(), 1);
            Assert.AreEqual(options.ExcludeBindings.First(), "经/已經.example.com");
        }

        [TestMethod]
        public void CommonNameExcluded()
        {
            var options = Options($"--siteid 1,2 --commonname test.example.com --excludebindings test.example.com,four.example.com");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void CommonNameExcludedAfter()
        {
            var siteId = 1;
            var site = iis.GetWebSite(siteId);
            var options = new IISSitesOptions() { SiteIds = new List<long>() { 1, 2 }, CommonName = "missing.example.com" };
            var target = Target(options);
            Assert.AreEqual(target.IsValid(log), true);
            Assert.AreEqual(target.CommonName, site.Bindings.First().Host);
        }

        [TestMethod]
        public void MissingSiteConfig()
        {
            var options = Options($"--siteid 1,999,2");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void MissingSiteExecution()
        {
            var options = new IISSitesOptions()
            {
                SiteIds = new List<long>() { 1, 999 }
            };
            var target = Target(options);
            Assert.AreEqual(target.Parts.Count(), 1);
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
            var options = Options($"--siteid 1,ab,2");
            Assert.IsNull(options);
        }
    }
}