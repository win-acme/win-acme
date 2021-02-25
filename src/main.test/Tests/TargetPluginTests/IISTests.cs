using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class IISTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly IISHelper helper;
        private readonly MockPluginService plugins;
        private readonly IUserRoleService userRoleService;
        private readonly DomainParseService domainParse;

        public IISTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            var settings = new MockSettingsService();
            var proxy = new ProxyService(log, settings);
            var domainParseService = new DomainParseService(log, proxy, settings);
            helper = new IISHelper(log, iis, domainParseService);
            plugins = new MockPluginService(log);
            userRoleService = new Mock.Services.UserRoleService();
        }

        private IISOptions? Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            var x = new IISOptionsFactory(log, helper, arguments, userRoleService);
            return x.Default().Result;
        }

        private Target Target(IISOptions options)
        {
            var plugin = new IIS(log, userRoleService, helper, options);
            return plugin.Generate().Result;
        }

        [DataRow("e")]
        [DataRow("e?")]
        [DataRow("e.")]
        [TestMethod]
        public void RegexPattern(string regex)
        {
            var options = Options($"--host-regex {regex}");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(regex, options.IncludeRegex?.ToString());
                var target = Target(options);
                Assert.IsNotNull(target);
                var allHosts = target.GetHosts(true);
                Assert.IsTrue(allHosts.All(x => Regex.Match(x, regex).Success));
            }
        }

        [DataRow("test")]
        [DataRow("alt.")]
        [TestMethod]
        public void Pattern(string pattern)
        {
            var options = Options($"--host-pattern *{pattern}*");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual($"*{pattern}*", options.IncludePattern?.ToString());
                var target = Target(options);
                Assert.IsNotNull(target);
                var allHosts = target.GetHosts(true);
                Assert.IsTrue(allHosts.All(x => x.Contains(pattern)));
            }
        }

        [TestMethod]
        public void RegexPlusPattern()
        {
            var options = Options($"--host-regex .+ --host-pattern *test*");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void RegexPlusIncludeHosts()
        {
            var options = Options($"--host-regex .+ --host test.example.com");
            Assert.IsNull(options);
        }

        [TestMethod]
        public void PatternPlusIncludeHosts()
        {
            var options = Options($"--host-pattern *test* --host test.example.com");
            Assert.IsNull(options);
        }
    }
}