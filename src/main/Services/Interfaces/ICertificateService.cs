using ACMESharp.Protocol;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Services
{
    internal interface ICertificateService
    {
        CertificateInfo CachedInfo(Renewal renewal);
        CertificateInfo RequestCertificate(ICsrPlugin csrPlugin, RunLevel runLevel, Renewal renewal, Target target, OrderDetails order);
        void RevokeCertificate(Renewal renewal);
    }
}