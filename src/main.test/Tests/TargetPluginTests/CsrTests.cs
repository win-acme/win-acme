using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class CsrTests
    {
        /// <summary>
        /// Generated using https://certificatetools.com/
        /// </summary>
        readonly string Csr = @"-----BEGIN CERTIFICATE REQUEST-----
MIIDSzCCAjMCAQAwWzEZMBcGA1UEAwwQd3d3Lndpbi1hY21lLmNvbTELMAkGA1UE
BhMCSFUxHzAdBgNVBAgMFkJvcnNvZC1BYmHDumotWmVtcGzDqW4xEDAOBgNVBAcM
B01pc2tvbGMwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDhDFr/lZBY
aew4dayZTDd4J7a0eMkUUQsAI3oNRwp4opaxTLretfJVGjdmEdHa0goKeLUFCUAJ
9aOe4ik0wY79MMaNPt8MPOnZDTLBc9bVqdDZI62GzbyJxgFZ1/QJWN3e0ZOd8TC9
P+UU+3KEJvZPaEs4FcI8MqCdO/Xx31BFuRH63odXPDYF6YMMegdp8ZkLsm3BR8Zl
9A0Rd/XrOJpO8tt19hvr0O11DbSDZ2FSAZzoJ+GOw8hUtKlju/dj0iOJxYNj1aTx
qBLVNnT02tIhHAaEbiHtwfOybGuPDdRt/NB/D6vYjGEVSVwr/mvd95aI+h8SZ/Ra
EgWNyULO0dhnAgMBAAGggaowgacGCSqGSIb3DQEJDjGBmTCBljAOBgNVHQ8BAf8E
BAMCBaAwIAYDVR0lAQH/BBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMGIGA1UdEQRb
MFmHBAEBAQGHBAECAwSHEGhNERECIjMzRERVVQAGAHeCDHdpbi1hY21lLmNvbYIQ
d3d3Lndpbi1hY21lLmNvbYEZd2luLmFjbWUuc2ltcGxlQGdtYWlsLmNvbTANBgkq
hkiG9w0BAQsFAAOCAQEAxZ5EiV6M17v2pW1wJJXbI/1KKMhY05gyPq+pHNal5qRE
rArwt9y/WISNmX+PUsMBEqUqZtNFdP/oMwcqLjfV4stL6mFCmHhy/X2X6VR3G6SC
qfXgA/fJq+14DqnnC1p4Ww/65xAE8br8QHxVZ5G5k9RQ6+Tfs22sVJLjt7UdN7yu
cz+2LLTM86nhWmffpNR+C+C4wmB7Sq9zN3ty5Qn1e7yVKJIn3duQYgMoIEEGdhP9
pT4M4qdY0+SDgKptoHhBEAeeCbBMYewBH/5l1qMd7KWROiNxA5Gt00TSX3xkz/bY
ZkoLUgEWU5OcCkq5AIpmloeaCTC/vKrlS5M3BvjEmQ==
-----END CERTIFICATE REQUEST-----";

        [TestMethod]
        public void Regular()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, Csr);
                var csrOptions = new CsrOptions() { CsrFile = tempFile };
                var log = new Mock.Services.LogService(false);
                var pem = new PemService();
                var csrPlugin = new Csr(log, pem, csrOptions);
                var target = csrPlugin.Generate().Result;
                Assert.IsNotNull(target);
                Assert.IsFalse(target is INull);
                Assert.IsTrue(target.Parts.Count() == 1);
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<IpIdentifier>().Count() == 3);
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<DnsIdentifier>().Count() == 2);
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<EmailIdentifier>().Count() == 1);
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<IpIdentifier>().Any(x => x.Value == "1.1.1.1"));
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<DnsIdentifier>().Any(x => x.Value == "www.win-acme.com"));
                Assert.IsTrue(target.Parts.First().Identifiers.OfType<EmailIdentifier>().Any(x => x.Value == "win.acme.simple@gmail.com"));

            } 
            finally
            {
                File.Delete(tempFile);
            }
        }

    }
}
