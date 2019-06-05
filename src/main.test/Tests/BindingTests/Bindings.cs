using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly byte[] oldCert1 = new byte[] { 0x1 };
        private readonly byte[] scopeCert = new byte[] { 0x0 };

        private const string httpOnlyHost = "httponly.example.com";
        private const long httpOnlyId = 1;

        private const string regularHost = "regular.example.com";
        private const long regularId = 2;

        private const string outofscopeHost = "outofscope.example.com";
        private const long outofscopeId = 3;

        private const string inscopeHost = "inscope.example.com";
        private const long inscopeId = 4;

        private const long piramidId = 5;

        private const string sniTrapHost = "snitrap.example.com";
        private const long sniTrap1 = 6;
        private const long sniTrap2 = 7;

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
                            CertificateHash = oldCert1,
                            CertificateStoreName = AltStore,
                            SSLFlags = SSLFlags.None
                        }
                    }
                },
                new MockSite() {
                    Id = inscopeId,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = DefaultPort,
                            Host = inscopeHost,
                            Protocol = "https",
                            CertificateHash = scopeCert,
                            CertificateStoreName = DefaultStore,
                            SSLFlags = SSLFlags.SNI
                        }
                    }
                },
                new MockSite() {
                    Id = outofscopeId,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = DefaultPort,
                            Host = outofscopeHost,
                            Protocol = "https",
                            CertificateHash = scopeCert,
                            CertificateStoreName = DefaultStore,
                            SSLFlags = SSLFlags.SNI
                        }
                    }
                },
                new MockSite() {
                    Id = piramidId,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = 80,
                            Host = "a.b.c.com",
                            Protocol = "http"
                        },
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = 80,
                            Host = "*.b.c.com",
                            Protocol = "http"
                        },
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = 80,
                            Host = "*.c.com",
                            Protocol = "http"
                        },
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = 80,
                            Host = "*.com",
                            Protocol = "http"
                        },
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = 80,
                            Host = "",
                            Protocol = "http"
                        }
                    }
                },
                new MockSite() {
                    Id = sniTrap1,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = DefaultPort,
                            Host = sniTrapHost,
                            Protocol = "https",
                            CertificateHash = oldCert1,
                            CertificateStoreName = DefaultStore,
                            SSLFlags = SSLFlags.None
                        }
                    }
                },
                new MockSite() {
                    Id = sniTrap2,
                    Bindings = new List<MockBinding> {
                        new MockBinding() {
                            IP = DefaultIP,
                            Port = DefaultPort,
                            Host = "",
                            Protocol = "https",
                            CertificateHash = oldCert1,
                            CertificateStoreName = DefaultStore,
                            SSLFlags = SSLFlags.None
                        }
                    }
                },
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
                WithSiteId(httpOnlyId).
                WithIP(bindingIp).
                WithPort(bindingPort).
                WithStore(storeName).
                WithFlags(inputFlags).
                WithThumbprint(newCert);

            var httpOnlySite = iis.GetWebSite(httpOnlyId);
            iis.AddOrUpdateBindings(new[] { testHost }, bindingOptions, oldCert1);
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
                WithSiteId(regularId).
                WithIP(bindingIp).
                WithPort(bindingPort).
                WithStore(storeName).
                WithFlags(inputFlags).
                WithThumbprint(newCert);

            var regularSite = iis.GetWebSite(regularId);
            iis.AddOrUpdateBindings(new[] { regularHost }, bindingOptions, oldCert1);
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

        [TestMethod]
        public void UpdateOutOfScope()
        {
            var iis = GetIISClient();

            var bindingOptions = new BindingOptions().
                WithSiteId(inscopeId).
                WithIP(DefaultIP).
                WithPort(DefaultPort).
                WithStore(DefaultStore).
                WithThumbprint(newCert);

            var outofScopeSite = iis.GetWebSite(outofscopeId);
            iis.AddOrUpdateBindings(new[] { regularHost }, bindingOptions, scopeCert);
            Assert.AreEqual(1, outofScopeSite.Bindings.Count);

            var updatedBinding = outofScopeSite.Bindings[0];
            Assert.AreEqual(DefaultStore, updatedBinding.CertificateStoreName);
            Assert.AreEqual(newCert, updatedBinding.CertificateHash);
        }

        [TestMethod]
        [DataRow("a.b.c.com", new string[] { }, "a.b.c.com")]
        [DataRow("a.b.c.com", new[] { "a.b.c.com" }, "*.b.c.com")]
        [DataRow("a.b.c.com", new[] { "a.b.c.com", "*.b.c.com" }, "*.c.com")]
        [DataRow("a.b.c.com", new[] { "a.b.c.com", "*.b.c.com", "*.c.com" }, "*.com")]
        [DataRow("a.b.c.com", new[] { "a.b.c.com", "*.b.c.com", "*.c.com", "*.com" }, "")]
        [DataRow("a.b.c.com", new[] { "a.b.c.com", "*.b.c.com", "*.c.com", "*.com", "" }, "a.b.c.com")]
        [DataRow("*.b.c.com", new string[] { }, "*.b.c.com")]
        [DataRow("*.b.c.com", new[] { "*.b.c.com" }, "a.b.c.com")]
        [DataRow("*.b.c.com", new[] { "a.b.c.com", "*.b.c.com", "*.c.com", "*.com", "" }, "*.b.c.com")]
        public void UpdatePiramid(string certificateHost, string[] ignoreBindings, string expectedBinding)
        {
            var iis = GetIISClient();

            var bindingOptions = new BindingOptions().
                WithSiteId(piramidId).
                WithIP(DefaultIP).
                WithPort(DefaultPort).
                WithStore(DefaultStore).
                WithThumbprint(newCert);

            var piramidSite = iis.GetWebSite(piramidId);
            var originalSet = piramidSite.Bindings.Where(x => !ignoreBindings.Contains(x.Host)).ToList();
            piramidSite.Bindings = originalSet.ToList().OrderBy(x => Guid.NewGuid()).ToList();
            iis.AddOrUpdateBindings(new[] { certificateHost }, bindingOptions, scopeCert);

            var newBindings = piramidSite.Bindings.Except(originalSet);
            Assert.AreEqual(1, newBindings.Count());

            var newBinding = newBindings.First();
            Assert.AreEqual(expectedBinding, newBinding.Host);
        }

        /// <summary>
        /// SNI should be turned on when an existing HTTPS binding
        /// without SNI is modified but another HTTPS binding, 
        /// also without SNI, is listening on the same IP and port.
        /// </summary>
        [TestMethod]
        public void SNITrap1()
        {
            var iis = GetIISClient();

            var bindingOptions = new BindingOptions().
                WithSiteId(sniTrap1).
                WithIP(DefaultIP).
                WithPort(DefaultPort).
                WithStore(DefaultStore).
                WithThumbprint(newCert);

            var sniTrap1Site = iis.GetWebSite(sniTrap1);
            var sniTrap2Site = iis.GetWebSite(sniTrap2);
            iis.AddOrUpdateBindings(new[] { sniTrapHost }, bindingOptions, scopeCert);
  
            var updatedBinding = sniTrap1Site.Bindings[0];
            Assert.AreEqual(SSLFlags.SNI, updatedBinding.SSLFlags);
            Assert.AreEqual(newCert, updatedBinding.CertificateHash);
        }

        /// <summary>
        /// Like above, but SNI cannot be turned on for the default
        /// website / empty host. The code should ignore the change.
        /// </summary>
        [TestMethod]
        public void SNITrap2()
        {
            var iis = GetIISClient();

            var bindingOptions = new BindingOptions().
                WithSiteId(sniTrap2).
                WithIP(DefaultIP).
                WithPort(DefaultPort).
                WithStore(DefaultStore).
                WithThumbprint(newCert);

            var sniTrap1Site = iis.GetWebSite(sniTrap1);
            var sniTrap2Site = iis.GetWebSite(sniTrap2);
            iis.AddOrUpdateBindings(new[] { sniTrapHost }, bindingOptions, scopeCert);

            var updatedBinding = sniTrap2Site.Bindings[0];
            Assert.AreEqual(SSLFlags.None, updatedBinding.SSLFlags);
            Assert.AreEqual(oldCert1, updatedBinding.CertificateHash);
        }

        // No fuzzy matching for central certificate store

        // Prevent duplicate binding

        // Prevent Windows 2008 collision

        // Don't allow wildcards in IIS <10
    }
}