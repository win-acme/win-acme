using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.X509;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using Serilog;
using System;
using System.Linq;
using System.Text;
using Net = System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.UnitTests.Tests.CertificateInfoTests
{
    [TestClass]
    public class CertificateInfoTests
    {
        public static CertificateInfo CloudFlare()
        {
            var cloudFlare = "MIIGZgIBAzCCBiIGCSqGSIb3DQEHAaCCBhMEggYPMIIGCzCCBgcGCSqGSIb3DQEHBqCCBfgwggX0AgEAMIIF7QYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQMwDgQI3ANcPRodpLYCAgfQgIIFwHKdqk8iUPoKm0wT3fb4cByo1e2iMV/ab/MdzgtoKEUbfgn4xSsyPK48ON9M7tAbeuCk5wSZO6V28O3fMPv5JgowvC5uctZC8i5pXv9/3MDZpy5tzX1jc0k3RRWEdpoumqV5KMW34CoDbUpwLb2L5BboCJ8Zqb1bPxf02qg3aAs0StftWuFg3i9n36g/nmwrawfRJLwpatOxy5SFE8ls0LHQFB/5Q90z/PwET3i6bMylEQ4CWI/+f+tTi3SbW2UcX7i8xDRDI6kz5aRpxv7Q/mxlNWW2Xw9Mk1lRP0bSiWUNkQNHipMbbhfJRTVmXpFJa2NnzZaiLoz9C8D27NnPrOleNCDAV3SSGEpUvgdoq/9NmD+/mxnDwfKpKaH6Tcobim0imYhMH2dQogg0k8AjxLIof8wPZYyFi0i7Grev+n8+vqp5IOMsVkGDF1VG1LZN5KskL7WIKH/UdMXOOLS2vrm2Zpgi5rr38nX92wI+l0FXhDSOlD/fos56BIijnYMS+dey6QMY0Ngu2rWWn33sd3m6OIlidvnfsKMPgLWiFUfQyPiLRUHq0PaY1SE/khNQiRckDIVd1S40NJVJ3qCW+c+fCjw6MJE0DgbzXVYy/yHAM6Mjm/KzuiAhJJD0vi2wOIlMS9ca4qyM+JNqc13abhsERbRGOEVaOmQ4gU30ZjheVzC89gAj+J7bZnl1Qs+X8c3Wivf3PYXZ+85QjmE90BZtZMuKTOID2NDlRkFKMhCSGubo33zjQ+SiqPltk18FTUwCXLQq5lG6j5i0qX/ODxAWq06GYg31M8x+c8x3zW2mbi7Zi9Z4Mes50MhqUgyUcV4sK4dcqAuesuOCsgqeKYRRfZShUxuXtoJUl02OHuG1kkB2ols49k+ZjzCv5ziH7xiMiXSDr+p6TIbiXltDsEP0KXq6890MJRK0wNS1NruGvL33Flo3DeE56cC16bwi0as5NMNUgQG7l03HzDpHOApA6bkZ/LzPvlGbkuW9ORngKLNW9eJE111mLuLB8vDyODBnrlx3PLsvJi/IIk9NjzU9Mah9OxVlvQK9dqSPiScZV3BDYi2YAYiMguH8WUGzn4fQuup0seFTCvU2L57sRhm9VqcBHB2o8mkE/kWf0wc6dlNtEjv4OvxdWIX0rE/W9bSQc1xdrwKM0qTOtW+iHiT3QsfEBLE72t8IJo5KoL2rf502gf+elMGiTkP+DLudAgdKJ7sW4GQdky6U2ZhbnY3gEkz2B9R9EiEplpL33H/z7YmUD1/bXYIxxDKfvhv52+8QBoyH+PPNrVDHPKNRm6pAK/H3QLARVi/TbFiwp/8ee235YzVFAU2UxHRLFusJwqjLkZjpHm/S8PRM8Mh3Ij7AdvviU2uEVHr7/5voaN5POIlW3hNVtFehMhmtZxLQqEwPVyVrAVp9LhCwrmrNqJAVZqPUklt+qf9eGBt6wDKI+LX6YuGRfS482M5V0cZEDqpjshYFhH6tU8t4SrxE/rAEExgTPTD35pPSyea8lIo/n/qrAnx6YIomzm1utugaDOMXfh3iUQRRGGkSBP35OfluzfiJ0Fk7Ob95aXrnVMcKPvm/F8cDhgLUoZpitfdySJua1YUPv1kpKvcv+GO1pL9FKIGiTRWcLW4KY1I61bjzLvDXh7TsgjVO0wPDTBxm3oGzp1pbSSS1zQ7YxyrHHZK7upyC3ATMtFguY7ODReS+pf/SpLhvmb01L3e0bvxVq+WcRgn9M5NzVSLQrC67XNqJwvmRSMvkqk7eSDvQNfQV32PAkrA377+mkgW2gMzKh28Y+mGVA1aA3PypJ13loP8bDfA0uZLMoPchT5Lm783dQvYB68vio3OlJoVzz7Ebr8vInmYCR01zz1Er0+ao9eU6U441Jov3ngpoAweGeSRvf6TvZ9Rp+DT0dcqiA7/vmRAFCvUh/VcGWRb6S7BZW2oh6gRwrlrvsShoxAy6h1OfMDswHzAHBgUrDgMCGgQUqoQ8vIJOQca0mk9zd2VgqiXPncgEFGzZDAZtg8QXWTpKypIutUbM+91qAgIH0A==";
            var tempPfx = new Net.X509Certificate2Collection();
            tempPfx.Import(
                Convert.FromBase64String(cloudFlare),
                null,
                Net.X509KeyStorageFlags.EphemeralKeySet |
                Net.X509KeyStorageFlags.Exportable);

            var pfxWrapper = PfxService.GetPfx();
            pfxWrapper.Store.SetCertificateEntry("1", new X509CertificateEntry(new X509Certificate(tempPfx[0].GetRawCertData())));
            var certinfo = new CertificateInfo(pfxWrapper);
            return certinfo;
        }

        [TestMethod]
        public void ParseIpWithIpAddresses()
        {
            var certinfo = CloudFlare();
            Assert.IsNotNull(certinfo);
            Assert.IsTrue(certinfo.SanNames.Any(x => x.Value == "1.1.1.1"));
        }

        [TestMethod]
        public void Chain()
        {
            var cert1 = @"-----BEGIN CERTIFICATE-----
MIIFQDCCAyigAwIBAgISSDYuE5PR5b3MzzlsZ1bBVGviMA0GCSqGSIb3DQEBCwUAMGsxCzAJBgNV
BAYTAlVTMSQwIgYDVQQKDBtJbmRpYW5hIFdlc2xleWFuIFVuaXZlcnNpdHkxCzAJBgNVBAsMAklU
MSkwJwYDVQQDDCBJbmRpYW5hIFdlc2xleWFuIFVuaXZlcnNpdHkgRVpDQTAeFw0yNDA1MjAxNzAz
MjhaFw0yNDA4MTcxOTAzMjhaMCYxJDAiBgNVBAMMG2Rldi1zcWwwMi5pd3VuZXQuaW5kd2VzLmVk
dTB2MBAGByqGSM49AgEGBSuBBAAiA2IABEnAcBZ4cTEkoB8BqStxg2pWQh1WRQpvaxFlVkbgnCNQ
zqpi+ZkwwYnJyU8riC3J5XZh2C3VEy1Fh5uwn8ThoUK7ROiMZ4AMKuqstWRicwxyVNFCMYgi5q41
TNU9oe320qOCAc8wggHLMB0GA1UdDgQWBBQfmIJqVJU57InO/sPzvJei8qFWWjAfBgNVHSMEGDAW
gBQgV5s7ke8XIQQhpJS2kSuZMT2w7jBeBggrBgEFBQcBAQRSMFAwTgYIKwYBBQUHMAKGQmh0dHA6
Ly9jZXJ0LmV6Y2EuaW8vY2VydHMvODEyYmIzMmEtZDExOC00ZDczLWJiZmMtMzlmODM2MTY5YTY3
LmNlcjBPBgNVHSUESDBGBggrBgEFBQcDAgYIKwYBBQUHAwEGCCsGAQUFBwMDBggrBgEFBQcDBAYI
KwYBBQUHAwUGCCsGAQUFBwMGBggrBgEFBQcDBzBlBgNVHREEXjBcghtkZXYtc3FsMDIuaXd1bmV0
LmluZHdlcy5lZHWCCWRldi1zcWwwMoIhd2FyZWhvdXNldGVzdGRiLml3dW5ldC5pbmR3ZXMuZWR1
gg93YXJlaG91c2V0ZXN0ZGIwDgYDVR0PAQH/BAQDAgWgMGEGA1UdHwRaMFgwVqBUoFKGUGh0dHA6
Ly9jcmwuZXpjYS5pby9jcmxzLzViMzk0ZmY1LTMwMTYtNGZjMS1iYTE0LWJlZjUxZWU4ZGZlZi9J
bmRpYW5hV2VzbGV5YW4uY3JsMA0GCSqGSIb3DQEBCwUAA4ICAQAdNwXz2u6OlJ0Cwji6jQlQ9H3h
w7jd4T8K9/6t28TtmyyNrcwhNma0bzk7UrhUeOEaIgm6QGRqQl4QI0XzF+EnaB/MIOYO2O1brsjk
IATY9yOzRmEYFflS4aWe0x+rIv9jLFdcrmvGKFR2ZqLu9iIhrDfy8CGVnShwu3vWtvdbHIdWXO3k
qhZF5rK/7MM5U7aCHzGLd6WPv6QkpHv/qpNfQ3dSdQGsx/ZTgJpyOlNQsiSrpk4+OJTAs2h49pZV
ZVAV9asecMaN0boRzGBH8ee4RCeZJlHyv6MVf0uUaZu4TRhnRX3jry6x2eCw0p7GaBeJbMVt/WvM
jQ0fxj/WfQSz5muqXmleGnVFA/N3SRB0IVBHIkGQfcC/CykNYzCr4fijzj8LTBBnikqZvg/LhXR+
n+ySbqKTtkTI/Rw15i1QdqMG/TdWRx8q+61UimBtj37VAuodpmwhiDQiJMxbCdSyl+3gkE2dqctX
O+ZecNy4oan0HXFsPe75TAHeYWVyNm/hftXobjIC9R74yfk3wH9zvLHrq9+0Ob+HU8AhZlpAch5j
OOvWZ5vwVx2SFPZgrcayrHqXE7qfblAmAIW0sokGdE+AKlTLzgd4hHb9qtdCmPah2q0IYo3YUuaN
KwcULZYBCoU/JGLwCP3SYXAD7T+oKieJpHAc4mvmsf1geLmRCQ==
-----END CERTIFICATE-----

                            -----BEGIN CERTIFICATE-----
MIIG8zCCBNugAwIBAgISN2ffX8oveG1xafWPYUqJlRb4MA0GCSqGSIb3DQEBCwUAMG4xCzAJBgNV
BAYTAlVTMSQwIgYDVQQKDBtJbmRpYW5hIFdlc2xleWFuIFVuaXZlcnNpdHkxCzAJBgNVBAsMAklU
MSwwKgYDVQQDDCNJbmRpYW5hIFdlc2xleWFuIFVuaXZlcnNpdHkgUm9vdCBDQTAeFw0yNDA0MTIx
NDQ2MDNaFw0yOTA0MTIxNDM1MzZaMGsxCzAJBgNVBAYTAlVTMSQwIgYDVQQKDBtJbmRpYW5hIFdl
c2xleWFuIFVuaXZlcnNpdHkxCzAJBgNVBAsMAklUMSkwJwYDVQQDDCBJbmRpYW5hIFdlc2xleWFu
IFVuaXZlcnNpdHkgRVpDQTCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKpbaHS6dBcx
w82h961uCADaieFuI7OFdet1wItMMXoCLyDBMPj9OiZiTtRXtejZts4gPT/MEncnf0Eh9IH98h88
cUSq1nol9ZiEGVpfvUWBD3/IsItuzN67RPmpXKC2GKYKBj7ogw4fFM9Rr0wA6K2BwTroc+oY+cTy
Zl0VVW0baLg2Ih+EnnurJCTqcOwAV8y8g2G4AaQ3Z/T2iHn0fqDkcaaIjmYFHw+K7QkFEr+Q8Bks
C/g5DuMLN3HwA/09uh1LL0AimgHrxgPpYHu8F0jwhTEkjFv1r9qhRTHgC0Z+hZuTwIFfmmK8vynl
4/t3ASHHO75dwvvcOL6MHLLwcdGp6IWK41iEe2m0D/AjDbkEdS3IL0wxZTM1pf3JWwIug7gNk5MM
kf1N2F7KpNSE0pSJG+TzqL9ZneCqF4fonZ/vy/rchAFpWsA0ZalCgSAdE1y7COrp+Am2CTQfpy8V
JWMmq/I46PHSD930D6ylO81ZsI3nvDMzhVTmZ9RQ3iZYkIWdJGnqhjfsBLd756rQWWY4MWQ3JvTi
NFGfOw5o4BoN1g3BEnWMcV0ua3KluQPiK2SPxgoxkC1qj+YyuDODsw5DAE2Mjqnf7grMt99BvGI3
RXmQBK3UnSMhDi4yLLZG6o/7F5p1P86LVS0fsQDNz0bGkglyvJdDpN5B30s4RjpFAgMBAAGjggGM
MIIBiDAdBgNVHQ4EFgQUIFebO5HvFyEEIaSUtpErmTE9sO4wHwYDVR0jBBgwFoAUPDrcmgWJbXhk
oyhaF2ekHHwrLaowXgYIKwYBBQUHAQEEUjBQME4GCCsGAQUFBzAChkJodHRwOi8vY2VydC5lemNh
LmlvL2NlcnRzLzA3OTA1NjIxLTUwMTQtNDI3MS04Y2E4LTVhNmYzMzZjZDk5YS5jZXIwEQYDVR0g
BAowCDAGBgRVHSAAMA4GA1UdDwEB/wQEAwIBpjBPBgNVHSUESDBGBggrBgEFBQcDAgYIKwYBBQUH
AwEGCCsGAQUFBwMDBggrBgEFBQcDBAYIKwYBBQUHAwUGCCsGAQUFBwMGBggrBgEFBQcDBzBhBgNV
HR8EWjBYMFagVKBShlBodHRwOi8vY3JsLmV6Y2EuaW8vY3Jscy8zMzYzOWUzNy0xYTg2LTRlNDYt
OWQ3NC1jYmExYjk3NTRmM2EvSW5kaWFuYVdlc2xleWFuLmNybDAPBgNVHRMBAf8EBTADAQH/MA0G
CSqGSIb3DQEBCwUAA4ICAQAMZdTrmIEd2Ii6BM0nVsz3Apu72sGBXaTYl6G0sq+m63+Sih+va7Ky
ak26ylfBNZzA//wX4oE8zjxC4OnERr1p1pOAq8STYaql3/9EsZO6d4fwjTOmtGVfsjrO1ph/KFbH
CHyf/6Y0Hlvu3n70VkROGY7MUr8AtnCNTpk3jJ7BvqTtSPCItgddmUAmbsJGT8pdQNuxlfW4y3Ic
ZUNGMEFxJNbFan8lIbZDEAc9Eo/nL6YYpssoTgVJhODmxiumuZaVCXLJoODe/oc/UPWWmHwgUA/1
N/qn4LsmUfKq4p5157UDYsBzxiatOSgYeV+4wEn2GrfNNhgCezdmPsYKIeSvM7zng7CmVMYnGwx0
hQux02puIdyh2tiy1TMfjim6RB36l9S4PIicS362tnkOKhkZQvVl3ukN9c6vEwAwxjR46UTNmhcu
wfz3sFjzMzOAKakHR/NOOIREEmmWSOYuCb/UbroEN3kBU+1UNtu/mV6Ztx3tvurQEFqK1Ovz20By
9TXOo+ZgvUrj3IbdC+ZbYtP0tndDj803e0IPrvyjMpNkZyb7qTwMpOtpkteSB8JFd81srpJMp9bW
tMiw0wa8E/0Kg/iXF3G2pxlZjrdangRqMJNowEWfSZNpQJPNuo3nXY46KDAY3Fvlc4fGglbmCMHr
ySvTEJU5PQCZj+ngoP+M7A==
-----END CERTIFICATE-----

-----BEGIN CERTIFICATE-----
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
btUK9CQkVXXUE5FDNjWBh/WxofE=
-----END CERTIFICATE-----";
            var store = ParseData(cert1);
            var certinfo = new CertificateInfo(store);
            var collection = certinfo.AsCollection(Net.X509KeyStorageFlags.EphemeralKeySet);
            var main = collection.FirstOrDefault(x => !collection.Any(y => x.Subject == y.Issuer));
            Assert.IsNotNull(main);
        }


        /// <summary> 
        /// Parse raw bytes returned from the server
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="friendlyName"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private PfxWrapper ParseData(string text)
        {
            var pfxBuilder = PfxService.GetPfx();
            var pfx = pfxBuilder.Store;
            var startIndex = 0;
            const string startString = "-----BEGIN CERTIFICATE-----";
            const string endString = "-----END CERTIFICATE-----";
            while (true)
            {
                startIndex = text.IndexOf(startString, startIndex, StringComparison.Ordinal);
                if (startIndex < 0)
                {
                    break;
                }
                var endIndex = text.IndexOf(endString, startIndex, StringComparison.Ordinal);
                if (endIndex < 0)
                {
                    break;
                }
                endIndex += endString.Length;
                var pem = text[startIndex..endIndex];
                var bcCertificate = PemService.ParsePem<X509Certificate>(pem);
                if (bcCertificate != null)
                {
                    var bcCertificateEntry = new X509CertificateEntry(bcCertificate);
                    var bcCertAlias = bcCertificateEntry.Certificate.SubjectDN.CommonName(true);
                    pfx.SetCertificateEntry(bcCertAlias, bcCertificateEntry);
                }

                // This should never happen, but is a sanity check
                // not to get stuck in an infinite loop
                if (endIndex <= startIndex)
                {
                    break;
                }
                startIndex = endIndex;
            }
            return pfxBuilder;
        }
    }
}
