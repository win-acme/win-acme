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
        Task<CertificateInfo> StorePfx(Order order, byte[] pfx);

        CertificateInfo? CachedInfo(Order order);
        IEnumerable<CertificateInfo> CachedInfos(Renewal renewal);
        IEnumerable<CertificateInfo> CachedInfos(Renewal renewal, Order order);
        void Encrypt();
    }
}