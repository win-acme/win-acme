using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        string ReuseKeyPath(Order order);
        CertificateInfo? CachedInfo(Order order);
        IEnumerable<CertificateInfo> CachedInfos(Renewal renewal);
        IEnumerable<CertificateInfo> CachedInfos(Renewal renewal, Order order);
        Task<CertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, RunLevel runLevel, Order order);
        Task RevokeCertificate(Renewal renewal);
        void Encrypt();
        void Delete(Renewal renewal);
    }
}