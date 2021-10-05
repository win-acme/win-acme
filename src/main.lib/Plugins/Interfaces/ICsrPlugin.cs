using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin : IPlugin
    {
        Task<AsymmetricCipherKeyPair> GetKeys();
        Task<Pkcs10CertificationRequest> GenerateCsr(string cacheFile, Target target);
        Task<X509Certificate2> PostProcess(X509Certificate2 certificate);
    }
}
