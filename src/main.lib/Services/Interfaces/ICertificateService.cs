using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        Task<ICertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, Order order);
        Task RevokeCertificate(Renewal renewal);
    }
} 