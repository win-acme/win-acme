using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin
    {
        AsymmetricCipherKeyPair GetKeys();
        Pkcs10CertificationRequest GenerateCsr(string cacheFile, string commonName, List<string> identifiers);
        AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey);
        bool CanConvert();
    }
}
