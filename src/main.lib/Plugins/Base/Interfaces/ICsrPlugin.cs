using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin : IPlugin
    {
        Task<AsymmetricCipherKeyPair> GetKeys();
        Task<Pkcs10CertificationRequest> GenerateCsr(Target target, string? keyFile);
    }
}
