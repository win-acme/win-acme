using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.UnitTests.Tests.CertificateInfoTests
{
    [TestClass]
    public class CertificateInfoTests
    {
        [TestMethod]
        public void ParseIpWithIpAddresses()
        {
            var cloudFlare = "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";
            var tempPfx = new X509Certificate2Collection();
            tempPfx.Import(
                Convert.FromBase64String(cloudFlare),
                null,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);
            var certinfo = new CertificateInfo(tempPfx[0]);
            Assert.IsNotNull(certinfo);
            Assert.IsTrue(certinfo.SanNames.Any(x => x.Value == "1.1.1.1"));
        }
    }
}
