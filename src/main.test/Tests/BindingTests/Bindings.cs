using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Clients;
using System.Collections.Generic;

namespace PKISharp.WACS.UnitTests.Tests.BindingTests
{
    [TestClass]
    public class BindingTests
    {
        private readonly ILogService log;

        public BindingTests()
        {
            log = new Mock.Services.LogService(false);
        }

        [TestMethod]
        public void Basic()
        {
            var newCert = new byte[] { 0x1 };
            var oldCert = new byte[] { 0x2 };
            var testHost = "test.example.com";
            var iis = new MockIISClient(log);
            var bindingOptions = new BindingOptions().
                WithSiteId(1).
                WithStore("My").
                WithThumbprint(newCert);

            iis.MockSites = new[] {
                new MockSite() {
                    Id = 1,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = "*",
                            Port = 80,
                            Host = testHost,
                            Protocol = "http"
                        }
                    }
                }
            };

            iis.AddOrUpdateBindings(new[] { testHost }, bindingOptions, oldCert);
            Assert.AreEqual(2, iis.MockSites[0].Bindings.Count);

            var newBinding = iis.MockSites[0].Bindings[1];
            Assert.AreEqual(testHost, newBinding.Host);
            Assert.AreEqual("https", newBinding.Protocol);
            Assert.AreEqual("My", newBinding.CertificateStoreName);
            Assert.AreEqual(newCert, newBinding.CertificateHash);
            Assert.AreEqual(IISClient.DefaultBindingPort, newBinding.Port);
            Assert.AreEqual(IISClient.DefaultBindingIp, newBinding.IP);
            Assert.AreEqual(SSLFlags.SNI, newBinding.SSLFlags);
        }
    }
}