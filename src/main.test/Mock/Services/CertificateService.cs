using Org.BouncyCastle.Pkcs;
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
            var builder = new Pkcs12StoreBuilder();
            var pfx = builder.Build();
            return Task.FromResult<ICertificateInfo>(new CertificateInfo(pfx));
        }
    }
}
