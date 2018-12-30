using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.UnitTests.Tests.Target
{
    [TestClass]
    public class IISBinding
    {
        [TestMethod]
        public void Regular()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 1 --host test.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.AreEqual(result.SiteId, 1, "Unexpected SiteId");
        }

        [TestMethod]
        public void IDNUnicode()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 1 --host 经/已經.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.AreEqual(result.SiteId, 1, "Unexpected SiteId");
        }

        [TestMethod]
        public void IDNAscii()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 1 --host xn--/-9b3b774gbbb.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.AreEqual(result.SiteId, 1, "Unexpected SiteId");
        }

        [TestMethod]
        public void WrongSite()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 2 --host test.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void WrongHost()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 1 --host doesntexist.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.IsNull(result);
        }
    }
}
