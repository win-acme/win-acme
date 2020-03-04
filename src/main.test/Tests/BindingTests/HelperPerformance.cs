using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Clients;
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
                    var randomId = ShortGuid.NewGuid().ToString();
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
            var helper = new IISHelper(log, iis);
            var timer = new Stopwatch();
            timer.Start();
            helper.GetSites(false);
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);

            timer.Reset();
            timer.Start();
            helper.GetBindings();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);
        }


    }
}
