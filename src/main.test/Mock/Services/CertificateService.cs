using ACMESharp.Protocol;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class CertificateService : ICertificateService
    {
        public CertificateInfo CachedInfo(Renewal renewal)
        {
            return null;
        }

        public CertificateInfo RequestCertificate(ICsrPlugin csrPlugin, RunLevel runLevel, Renewal renewal, Target target, OrderDetails order)
        {
            // Create self-signed certificate
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={target.CommonName}", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            return new CertificateInfo()
            {
                CacheFile = null,
                CacheFilePassword = null,
                Certificate = cert,
            };
        }

        public void RevokeCertificate(Renewal renewal)
        {
        }
    }
}
