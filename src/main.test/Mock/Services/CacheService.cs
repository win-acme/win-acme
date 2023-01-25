using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
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

        public IEnumerable<CertificateInfoCache> CachedInfos(Renewal renewal)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CertificateInfoCache> CachedInfos(Renewal renewal, Order order)
        {
            throw new NotImplementedException();
        }

        public void Delete(Order order) {}

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
    }
}
