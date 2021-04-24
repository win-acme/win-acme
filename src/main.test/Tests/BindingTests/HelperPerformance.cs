using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Clients;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PKISharp.WACS.UnitTests.Tests.BindingTests
{
    [TestClass]
    public class HelperPerformance
    {
        private readonly ILogService log;
        public HelperPerformance() => log = new Mock.Services.LogService(false);

        [TestMethod]
        public void Speed()
        {
            var iis = new MockIISClient(log, 10);
            var siteList = new List<MockSite>();
            for (var i = 0; i < 5000; i++)
            {
                var bindingList = new List<MockBinding>();
                for (var j = 0; j < 10; j++)
                {
                    var randomBindingOptions = new BindingOptions();
                    var randomId = "a" + ShortGuid.NewGuid().ToString() + ".nl";
                    randomBindingOptions = randomBindingOptions.WithHost(randomId.ToLower());
                    bindingList.Add(new MockBinding(randomBindingOptions));
                };
                siteList.Add(new MockSite()
                {
                    Id = i,
                    Bindings = bindingList
                });
            }
            iis.MockSites = siteList.ToArray();
            var settings = new MockSettingsService();
            var proxy = new Mock.Services.ProxyService();
            var domainParse = new DomainParseService(log, proxy, settings);
            var helper = new IISHelper(log, iis, domainParse);
            var timer = new Stopwatch();
            timer.Start();
            _ = helper.GetSites(false);
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);

            timer.Reset();
            timer.Start();
            _ = helper.GetBindings();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);
        }


    }
}
