using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class CertificateService : ICertificateService
    {
        public Task<ICertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, Order order)
        {
            // Create self-signed certificate
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN={order.Target.CommonName}", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            return Task.FromResult<ICertificateInfo>(new CertificateInfo(cert));
        }
    }
}
