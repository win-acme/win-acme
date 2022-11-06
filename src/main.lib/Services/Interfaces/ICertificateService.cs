using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        Task<CertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, RunLevel runLevel, Order order);
        Task RevokeCertificate(Renewal renewal);
    }
}