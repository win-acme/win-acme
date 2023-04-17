using Org.BouncyCastle.Crypto;
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
    public class CertificateInfo : ICertificateInfo
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
                var bytes = rawCollection.Export(X509ContentType.Pfx) ?? 
                    throw new InvalidOperationException();
                var builder = new Pkcs12StoreBuilder();
                var store = builder.Build();
                store.Load(new MemoryStream(bytes), "".ToCharArray());
                var alias = store.Aliases.OfType<string>().FirstOrDefault(store.IsKeyEntry);
                if (alias != null)
                {
                    var entry = store.GetKey(alias);
                    var key = entry.Key;
                    if (key.IsPrivate)
                    {
                        PrivateKey = key;
                    }
                }
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

        public X509Certificate2Collection Collection { get; private set; }

        public X509Certificate2 Certificate { get; private set; }

        public AsymmetricKeyParameter? PrivateKey { get; private set; }

        public IEnumerable<X509Certificate2> Chain { get; private set; }

        public Identifier CommonName
        {
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

        public IEnumerable<Identifier> SanNames => Certificate.SanNames();
    }
}
