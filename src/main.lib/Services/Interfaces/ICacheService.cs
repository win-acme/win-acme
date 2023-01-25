using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICacheService
    {
        FileInfo Key(Order order);

        void Delete(Renewal renewal);
        
        Task StoreCsr(Order order, string csr);
        Task<ICertificateInfo> StorePfx(Order order, byte[] pfx);

        CertificateInfoCache? CachedInfo(Order order);
        IEnumerable<CertificateInfoCache> CachedInfos(Renewal renewal);
        IEnumerable<CertificateInfoCache> CachedInfos(Renewal renewal, Order order);
        void Encrypt();
    }
}