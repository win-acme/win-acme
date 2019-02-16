using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPlugin
    {
        AsymmetricKeyParameter GetPrivateKey();
        CertificateRequest GenerateCsr(string commonName, List<string> identifiers);
        AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey);
        bool CanConvert();
    }
}
