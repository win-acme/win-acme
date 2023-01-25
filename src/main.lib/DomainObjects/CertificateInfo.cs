using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Provides information about a certificate, which may or may not already
    /// be stored on the disk somewhere in a .pfx file
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Shorthand constructor
        /// </summary>
        /// <param name="cert"></param>
        public CertificateInfo(X509Certificate2 cert) : this(new X509Certificate2Collection(cert)) { }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="rawCollection"></param>
        /// <exception cref="InvalidDataException"></exception>
        public CertificateInfo(X509Certificate2Collection rawCollection)
        {
            // Store original collection
            Collection = rawCollection;

            // Get first certificate that has not been used to issue 
            // another one in the collection. That is the outermost leaf
            // and thus will be our main certificate
            var list = rawCollection.OfType<X509Certificate2>().ToList();
            var main = list.FirstOrDefault(x => !list.Any(y => x.Subject == y.Issuer));
            if (main == null)
            {
                // Self-signed (unit test)
                main = list.FirstOrDefault();
                if (main == null)
                {
                    throw new InvalidDataException("Empty X509Certificate2Collection");
                }
            }
            Certificate = main;

            // Check if we have the private key
            if (main.HasPrivateKey)
            {

            }

            // Now order the remaining certificates in the correct order of 
            // who signed whom.
            list.Remove(main);
            var lastChainElement = main;
            var orderedCollection = new List<X509Certificate2>();
            while (list.Count > 0)
            {
                var signedBy = list.FirstOrDefault(x => lastChainElement.Issuer == x.Subject);
                if (signedBy == null)
                {
                    // Chain cannot be resolved any further
                    break;
                }
                orderedCollection.Add(signedBy);
                lastChainElement = signedBy;
                list.Remove(signedBy);
            }
            Chain = orderedCollection;
        }

        /// <summary>
        /// Entire collection, equivalent to the full PFX archive
        /// </summary>
        public X509Certificate2Collection Collection { get; private set; }

        /// <summary>
        /// The main certificate
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Private key in Bouncy Castle format
        /// </summary>
        public AsymmetricKeyEntry? PrivateKey { get; private set; }

        /// <summary>
        /// The certificate chain, in the correct order
        /// </summary>
        public IEnumerable<X509Certificate2> Chain { get; private set; }

        public FileInfo? CacheFile { get; set; }
        public string? CacheFilePassword { get; set; }

        /// <summary>
        /// The common name / subject name
        /// </summary>
        public Identifier CommonName {
            get
            {
                var str = Certificate.SubjectClean();
                if (string.IsNullOrWhiteSpace(str))
                {
                    return SanNames.First();
                }
                return new DnsIdentifier(str);
            }
        }

        /// <summary>
        /// The Subject Alternative Names (up to 100 for Let's Encrypt)
        /// that are also validate identifiers to be used with this
        /// certificate.
        /// </summary>
        public IEnumerable<Identifier> SanNames => Certificate.SanNames();

        /// <summary>
        /// This is used by the store plugins to communicate to the 
        /// installation plugins. Should be refactored at some point
        /// to be less spaghetti-like.
        /// </summary>
        public Dictionary<Type, StoreInfo> StoreInfo { get; private set; } = new Dictionary<Type, StoreInfo>();
    }



    /// <summary>
    /// Information about where the certificate is stored
    /// </summary>
    public class StoreInfo
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
    }
}
