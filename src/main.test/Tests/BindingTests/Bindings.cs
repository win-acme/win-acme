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

        private const string DefaultIP = IISClient.DefaultBindingIp;
        private const string AltIP = "1.1.1.1";

        private const string DefaultStore = "My";
        private const string AltStore = "WebHosting";

        private const int DefaultPort = IISClient.DefaultBindingPort;
        private const int AltPort = 1234;

        private readonly byte[] newCert = new byte[] { 0x2 };
        private readonly byte[] oldCert = new byte[] { 0x1 };

        private const string httpOnlyHost = "httponly.example.com";
        private const long httpOnlyId = 1;

        private const string regularHost = "regular.example.com";
        private const long regularId = 2;

        public BindingTests()
        {
            log = new Mock.Services.LogService(false);
        }

        private MockIISClient GetIISClient()
        {
            var iis = new MockIISClient(log)
            {
                MockSites = new[] {
                new MockSite() {
                    Id = httpOnlyId,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = "*",
                            Port = 80,
                            Host = httpOnlyHost,
                            Protocol = "http"
                        }
                    }
                },
                new MockSite() {
                    Id = regularId,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = "*",
                            Port = 80,
                            Host = regularHost,
                            Protocol = "http"
                        },
                        new MockBinding() {
                            IP = AltIP,
                            Port = AltPort,
                            Host = regularHost,
                            Protocol = "https",
                            CertificateHash = oldCert,
                            CertificateStoreName = AltStore,
                            SSLFlags = SSLFlags.None
                        }
                    }
                }
            }
            };
            return iis;
        }

        [TestMethod]
        // Basic
        [DataRow(DefaultStore, DefaultIP, DefaultPort, SSLFlags.None, SSLFlags.SNI)]
        // Alternative store
        [DataRow(AltStore, DefaultIP, DefaultPort, SSLFlags.None, SSLFlags.SNI)]
        // Alternative IP
        [DataRow(DefaultStore, AltIP, DefaultPort, SSLFlags.None, SSLFlags.SNI)]
        // Alternative port
        [DataRow(DefaultStore, DefaultIP, AltPort, SSLFlags.None, SSLFlags.SNI)]
        // Alternative flags
        [DataRow(DefaultStore, DefaultIP, DefaultPort, SSLFlags.CentralSSL, SSLFlags.SNI | SSLFlags.CentralSSL)]
        public void AddNewSimple(string storeName, string bindingIp, int bindingPort, SSLFlags inputFlags, SSLFlags expectedFlags)
        {
            var iis = GetIISClient();
            var testHost = httpOnlyHost;
            var bindingOptions = new BindingOptions().
                WithSiteId(1).
                WithIP(bindingIp).
                WithPort(bindingPort).
                WithStore(storeName).
                WithFlags(inputFlags).
                WithThumbprint(newCert);

            var httpOnlySite = iis.GetWebSite(httpOnlyId);
            iis.AddOrUpdateBindings(new[] { testHost }, bindingOptions, oldCert);
            Assert.AreEqual(2, httpOnlySite.Bindings.Count);

            var newBinding = httpOnlySite.Bindings[1];
            Assert.AreEqual(testHost, newBinding.Host);
            Assert.AreEqual("https", newBinding.Protocol);
            Assert.AreEqual(storeName, newBinding.CertificateStoreName);
            Assert.AreEqual(newCert, newBinding.CertificateHash);
            Assert.AreEqual(bindingPort, newBinding.Port);
            Assert.AreEqual(bindingIp, newBinding.IP);
            Assert.AreEqual(expectedFlags, newBinding.SSLFlags);
        }

        [TestMethod]
        // Basic
        [DataRow(DefaultStore, DefaultIP, DefaultPort, SSLFlags.None, SSLFlags.None)]
        // Alternative store
        [DataRow(AltStore, DefaultIP, DefaultPort, SSLFlags.None, SSLFlags.None)]
        // Alternative IP
        [DataRow(DefaultStore, AltIP, DefaultPort, SSLFlags.None, SSLFlags.None)]
        // Alternative port
        [DataRow(DefaultStore, DefaultIP, AltPort, SSLFlags.None, SSLFlags.None)]
        // Alternative flags
        [DataRow(DefaultStore, DefaultIP, DefaultPort, SSLFlags.CentralSSL, SSLFlags.CentralSSL)]
        public void UpdateSimple(string storeName, string bindingIp, int bindingPort, SSLFlags inputFlags, SSLFlags expectedFlags)
        {
            var iis = GetIISClient();
          
            var bindingOptions = new BindingOptions().
                WithSiteId(1).
                WithIP(bindingIp).
                WithPort(bindingPort).
                WithStore(storeName).
                WithFlags(inputFlags).
                WithThumbprint(newCert);

            var regularSite = iis.GetWebSite(regularId);
            iis.AddOrUpdateBindings(new[] { regularHost }, bindingOptions, oldCert);
            Assert.AreEqual(2, regularSite.Bindings.Count);

            var updatedBinding = regularSite.Bindings[1];
            Assert.AreEqual(regularHost, updatedBinding.Host);
            Assert.AreEqual("https", updatedBinding.Protocol);
            Assert.AreEqual(storeName, updatedBinding.CertificateStoreName);
            Assert.AreEqual(newCert, updatedBinding.CertificateHash);
            Assert.AreEqual(AltPort, updatedBinding.Port);
            Assert.AreEqual(AltIP, updatedBinding.IP);
            Assert.AreEqual(expectedFlags, updatedBinding.SSLFlags);
        }
    }
}