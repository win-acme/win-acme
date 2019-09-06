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
    public class ManualTests
    {
        private readonly ILogService log;
        private readonly IIISClient iis;
        private readonly PluginService plugins;

        public ManualTests()
        {
            log = new Mock.Services.LogService(false);
            iis = new Mock.Clients.MockIISClient(log);
            plugins = new PluginService(log);
        }

        private ManualOptions Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            var x = new ManualOptionsFactory(arguments);
            return x.Default();
        }

        private Target Target(ManualOptions options)
        {
            var plugin = new Manual(log, options);
            return plugin.Generate();
        }

        [TestMethod]
        public void Regular()
        {
            var options = Options($"--host a.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "a.example.com");
            Assert.AreEqual(options.AlternativeNames.Count(), 3);
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void Puny()
        {
            var options = Options($"--host xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.First(), "经/已經.example.com");
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void PunyWildcard()
        {
            var options = Options($"--host *.xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "*.经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.First(), "*.经/已經.example.com");
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void PunySubDomain()
        {
            var options = Options($"--host xn--/-9b3b774gbbb.xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "经/已經.经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.First(), "经/已經.经/已經.example.com");
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void UniWildcard()
        {
            var options = Options($"--host *.经/已經.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "*.经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.First(), "*.经/已經.example.com");
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void UniSubDomain()
        {
            var options = Options($"--host 经/已經.经/已經.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "经/已經.经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.First(), "经/已經.经/已經.example.com");
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void CommonNameUni()
        {
            var options = Options($"--commonname common.example.com --host a.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "common.example.com");
            Assert.AreEqual(options.AlternativeNames.Count(), 4);
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        public void CommonNamePuny()
        {
            var options = Options($"--commonname xn--/-9b3b774gbbb.example.com --host 经/已經.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.CommonName, "经/已經.example.com");
            Assert.AreEqual(options.AlternativeNames.Count(), 3);
            var tar = Target(options);
            Assert.AreEqual(tar.IsValid(log), true);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void NoHost()
        {
            var options = Options($"");
        }
    }
}