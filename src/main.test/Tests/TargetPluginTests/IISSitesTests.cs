using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISSitesTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISHelper helper;
        private readonly PluginService plugins;
        private readonly IUserRoleService userRoleService;
        private readonly DomainParseService domainParse;

        public IISSitesTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            var settings = new MockSettingsService();
            var proxy = new Mock.Services.ProxyService();
            domainParse = new DomainParseService(log, proxy, settings);
            helper = new IISHelper(log, iis, domainParse);
            plugins = new PluginService(log, new MockAssemblyService(log));
            userRoleService = new Services.UserRoleService(iis, new AdminService());
        }

        private IISOptions? Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, new MockAssemblyService(log), commandLine.Split(' '));
            var input = new Mock.Services.InputService(new());
            var secretService = new SecretServiceManager(new Mock.Services.SecretService(), input, log);
            var argsInput = new ArgumentsInputService(log, optionsParser, input, secretService);
            var args = new MainArguments();
            var x = new IISOptionsFactory(log, helper, args, argsInput);
            return x.Default().Result;
        }

        private Target? Target(IISOptions options)
        {
            var plugin = new IIS(log, helper, options);
            return plugin.Generate().Result;
        }

        [TestMethod]
        public void Regular()
        {
            var siteIdA = 1;
            var siteIdB = 2;
            var siteA = iis.GetSite(siteIdA);
            var siteB = iis.GetSite(siteIdB);
            var options = Options($"--siteid {siteIdA},{siteIdB}");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.IsNotNull(options.IncludeSiteIds);
                if (options.IncludeSiteIds != null)
                {
                    Assert.AreEqual(options.IncludeSiteIds.Contains(siteIdA), true);
                    Assert.AreEqual(options.IncludeSiteIds.Contains(siteIdB), true);
                    Assert.IsNull(options.CommonName);
                    Assert.IsNull(options.ExcludeHosts);

                    var target = Target(options);
                    Assert.IsNotNull(target);
                    Assert.AreEqual(target.IsValid(log), true);
                    Assert.AreEqual(target.CommonName.Value, siteA.Bindings.First().Host); // First binding
                    Assert.AreEqual(target.IIS, true);
                    Assert.AreEqual(target.Parts.Count, 2);
                    Assert.AreEqual(target.Parts.First().SiteId, siteIdA);
                    Assert.AreEqual(target.Parts.First().Identifiers.Count, siteA.Bindings.Count());
                    Assert.AreEqual(target.Parts.First().Identifiers.All(x => siteA.Bindings.Any(b => b.Host == x.Value)), true);

                    Assert.AreEqual(target.Parts.Last().SiteId, siteIdB);
                    Assert.AreEqual(target.Parts.Last().Identifiers.Count, siteB.Bindings.Count());
                    Assert.AreEqual(target.Parts.Last().Identifiers.All(x => siteB.Bindings.Any(b => b.Host == x.Value)), true);
                }
            }
           
        }

        [TestMethod]
        public void CommonNameUni()
        {
            var commonName = "经/已經.example.com";
            var options = Options($"--siteid 1,2 --commonname {commonName}");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, commonName);
                Assert.IsNull(options.ExcludeHosts);
                var target = Target(options);
                Assert.IsNotNull(target);
                Assert.AreEqual(target.IsValid(log), true);
                Assert.AreEqual(target.CommonName.Value, commonName);
            }
        }

        [TestMethod]
        public void CommonNamePuny()
        {
            var punyHost = "xn--/-9b3b774gbbb.example.com";
            var uniHost = "经/已經.example.com";
            var options = Options($"--siteid 1,2 --commonname {punyHost}");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, uniHost);
                Assert.IsNull(options.ExcludeHosts);
                var target = Target(options);
                Assert.IsNotNull(target);
                Assert.AreEqual(target.IsValid(log), true);
                Assert.AreEqual(target.CommonName.Value, uniHost);
            }
        }

        [TestMethod]
        public void ExcludeBindings()
        {
            var siteIdA = 1;
            var siteA = iis.GetSite(siteIdA);
            var options = Options($"--siteid 1,2 --excludebindings {siteA.Bindings.ElementAt(0).Host},four.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.IsNotNull(options.ExcludeHosts);
                Assert.AreEqual(options.ExcludeHosts?.Count, 2);
                var target = Target(options);
                Assert.IsNotNull(target);
                Assert.AreEqual(target.IsValid(log), true);
                Assert.AreEqual(target.CommonName.Value, siteA.Bindings.ElementAt(1).Host); // 2nd binding, first is excluded
            }
        }

        [TestMethod]
        public void ExcludeBindingsPuny()
        {
            var options = Options($"--siteid 1,2 --excludebindings xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.IsNotNull(options.ExcludeHosts);
                Assert.AreEqual(options.ExcludeHosts?.Count, 1);
                Assert.AreEqual(options.ExcludeHosts?.First(), "经/已經.example.com");
            }
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
            var site = iis.GetSite(siteId);
            var options = new IISSitesOptions() { SiteIds = new List<long>() { 1, 2 }, CommonName = "missing.example.com" };
            var target = Target(options);
            Assert.IsNotNull(target);
            Assert.AreEqual(true, target.IsValid(log));
            Assert.AreEqual(site.Bindings.First().Host, target.CommonName.Value);
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
            Assert.IsNotNull(target);
            Assert.AreEqual(1, target.Parts.Count);
        }


        [TestMethod]
        public void NoOptions()
        {
            var options = Options($"");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void IllegalSite()
        {
            var options = Options($"--siteid 1,ab,2");
            Assert.IsNull(options);
        }
    }
}