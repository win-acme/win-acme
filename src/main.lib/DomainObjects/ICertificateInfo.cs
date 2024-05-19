using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.Collections.Generic;

namespace PKISharp.WACS.DomainObjects
{
    public interface ICertificateInfo
    {
        /// <summary>
        /// Entire collection, equivalent to the full PFX archive
        /// </summary>
        Pkcs12Store Collection { get; }

        /// <summary>
        /// The main certificate
        /// </summary>
        X509Certificate Certificate { get; }

        /// <summary>
        /// Private key in Bouncy Castle format
        /// </summary>
        AsymmetricKeyParameter? PrivateKey { get; }

        /// <summary>
        /// The certificate chain, in the correct order
        /// </summary>
        IEnumerable<X509Certificate> Chain { get; }

        /// <summary>
        /// The common name / subject name
        /// </summary>
        Identifier? CommonName { get; }

        /// <summary>
        /// The Subject Alternative Names (up to 100 for Let's Encrypt)
        /// that are also validate identifiers to be used with this
        /// certificate.
        /// </summary>
        IEnumerable<Identifier> SanNames { get; }

        /// <summary>
        /// FriendlyName
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Main certificate hash
        /// </summary>
        byte[] GetHash();

        /// <summary>
        /// Main certificate thumbprint
        /// </summary>
        string Thumbprint { get; }
    }
}