using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin
    {
        Task<AsymmetricCipherKeyPair> GetKeys();
        Task<Pkcs10CertificationRequest> GenerateCsr(string cacheFile, string commonName, List<string> identifiers);
        Task<AsymmetricAlgorithm> Convert(AsymmetricAlgorithm privateKey);
        bool CanConvert();
    }
}
