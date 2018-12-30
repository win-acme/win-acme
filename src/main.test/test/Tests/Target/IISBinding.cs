using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Tests.Target
{
    [TestClass]
    public class IISBinding
    {
        [TestMethod]
        public void Default()
        {
            var log = new Mock.Services.LogService();
            var iisClient = new Mock.Clients.MockIISClient();
            var bindingHelper = new IISBindingHelper(log, iisClient);
            var x = new IISBindingOptionsFactory(log, iisClient, bindingHelper);
            var optionsParser = new OptionsParser(log, "--siteid 1 --manualhost test.example.com".Split(' '));
            var optionsService = new OptionsService(log, optionsParser.Options);
            var result = x.Default(optionsService);
            Assert.AreEqual(result.SiteId, 1, "Unexpected SiteId");
        }
    }
}
