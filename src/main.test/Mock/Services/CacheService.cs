using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class CacheService : ICacheService
    {
        public CertificateInfoCache? CachedInfo(Order order)
        {
            throw new NotImplementedException();
        }

        public CertificateInfoCache? PreviousInfo(Renewal renewal, string order)
        {
            throw new NotImplementedException();
        }

        public void Delete(Renewal renewal) {}

        public void Encrypt()
        {
            throw new NotImplementedException();
        }

        public FileInfo Key(Order order)
        {
            throw new NotImplementedException();
        }

        public Task StoreCsr(Order order, string csr)
        {
            throw new NotImplementedException();
        }

        public Task<ICertificateInfo> StorePfx(Order order, CertificateOption option)
        {
            throw new NotImplementedException();
        }

        public void Delete(Renewal renewal, string order)
        {
            throw new NotImplementedException();
        }

        public void Revoke(Renewal renewal)
        {
            throw new NotImplementedException();
        }
    }
}
