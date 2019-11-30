using ACMESharp.Protocol;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        CertificateInfo? CachedInfo(Renewal renewal, Target? target = null);
        Task<CertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, RunLevel runLevel, Renewal renewal, Target target, OrderDetails order);
        Task RevokeCertificate(Renewal renewal);
        void Encrypt();
        void Delete(Renewal renewal);
    }
}