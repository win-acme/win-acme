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
        void Delete(Renewal renewal, string order);

        Task StoreCsr(Order order, string csr);
        Task<ICertificateInfo> StorePfx(Order order, CertificateOption option);

        CertificateInfoCache? CachedInfo(Order order);
        CertificateInfoCache? PreviousInfo(Renewal renewal, string order);

        void Encrypt();
        void Revoke(Renewal renewal);
    }
}