using ACMESharp.Protocol;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        string CacheKey(Order order);
        CertificateInfo? CachedInfo(Order order);
        IEnumerable<CertificateInfo> CachedInfos(Renewal renewal);
        Task<CertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, RunLevel runLevel, Order order);
        Task RevokeCertificate(Renewal renewal);
        void Encrypt();
        void Delete(Renewal renewal);
    }
}