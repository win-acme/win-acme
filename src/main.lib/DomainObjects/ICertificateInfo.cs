using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.DomainObjects
{
    public interface ICertificateInfo
    {
        /// <summary>
        /// The main certificate
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// The certificate chain, in the correct order
        /// </summary>
        IEnumerable<X509Certificate2> Chain { get; }

        /// <summary>
        /// Entire collection, equivalent to the full PFX archive
        /// </summary>
        X509Certificate2Collection Collection { get; }

        /// <summary>
        /// The common name / subject name
        /// </summary>
        Identifier? CommonName { get; }

        /// <summary>
        /// Private key in Bouncy Castle format
        /// </summary>
        AsymmetricKeyParameter? PrivateKey { get; }

        /// <summary>
        /// The Subject Alternative Names (up to 100 for Let's Encrypt)
        /// that are also validate identifiers to be used with this
        /// certificate.
        /// </summary>
        IEnumerable<Identifier> SanNames { get; }
    }
}