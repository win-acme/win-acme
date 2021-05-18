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

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class ManualTests
    {
        private readonly ILogService log;
        private readonly IPluginService plugins;

        public ManualTests()
        {
            log = new Mock.Services.LogService(false);
            plugins = new MockPluginService(log);
        }

        private ManualOptions? Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var input = new Mock.Services.InputService(new());
            var secretService = new SecretServiceManager(new SecretService(), input, log);
            var argsInput = new ArgumentsInputService(log, optionsParser, input, secretService);
            var x = new ManualOptionsFactory(argsInput);
            return x.Default().Result;
        }

        private Target Target(ManualOptions options)
        {
            var plugin = new Manual(options);
            return plugin.Generate().Result;
        }

        [TestMethod]
        public void Regular()
        {
            var options = Options($"--host a.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "a.example.com");
                Assert.AreEqual(options.AlternativeNames.Count(), 3);
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void Puny()
        {
            var options = Options($"--host xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.First(), "经/已經.example.com");
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void PunyWildcard()
        {
            var options = Options($"--host *.xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "*.经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.First(), "*.经/已經.example.com");
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void PunySubDomain()
        {
            var options = Options($"--host xn--/-9b3b774gbbb.xn--/-9b3b774gbbb.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "经/已經.经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.First(), "经/已經.经/已經.example.com");
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void UniWildcard()
        {
            var options = Options($"--host *.经/已經.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "*.经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.First(), "*.经/已經.example.com");
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void UniSubDomain()
        {
            var options = Options($"--host 经/已經.经/已經.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "经/已經.经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.First(), "经/已經.经/已經.example.com");
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void IpAddress()
        {
            var options = Options($"--host abc.com,1.2.3.4");
            Assert.IsNotNull(options);
            if (options != null)
            {
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
                Assert.IsTrue(tar.Parts.First().Identifiers.OfType<IpIdentifier>().First().Value == "1.2.3.4");
            }
        }

        [TestMethod]
        public void CommonNameUni()
        {
            var options = Options($"--commonname common.example.com --host a.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "common.example.com");
                Assert.AreEqual(options.AlternativeNames.Count(), 4);
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        public void CommonNamePuny()
        {
            var options = Options($"--commonname xn--/-9b3b774gbbb.example.com --host 经/已經.example.com,b.example.com,c.example.com");
            Assert.IsNotNull(options);
            if (options != null)
            {
                Assert.AreEqual(options.CommonName, "经/已經.example.com");
                Assert.AreEqual(options.AlternativeNames.Count(), 3);
                var tar = Target(options);
                Assert.AreEqual(tar.IsValid(log), true);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void NoHost() => Options($"");
    }
}