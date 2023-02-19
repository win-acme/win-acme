using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Special implementation of CertificateInfo which contains reference
    /// to a file in the cache
    /// </summary>
    public class CertificateInfoCache : ICertificateInfo
    {
        private readonly CertificateInfo _inner;

        public CertificateInfoCache(FileInfo file, string? password)  
        {
            CacheFile = file;
            CacheFilePassword = password;

            // Flags used to read the X509Certificate2 
            var externalFlags =
                X509KeyStorageFlags.EphemeralKeySet |
                X509KeyStorageFlags.Exportable;
            var ret = new X509Certificate2Collection();
            ret.Import(file.FullName, password, externalFlags);
            _inner = new CertificateInfo(ret);
        }  

        /// <summary>
        /// Location on disk
        /// </summary>
        public FileInfo CacheFile { get; private set; }

        /// <summary>
        /// Password used to protect the file on disk
        /// </summary>
        public string? CacheFilePassword { get; private set; }

        public X509Certificate2 Certificate => _inner.Certificate;
        public IEnumerable<X509Certificate2> Chain => _inner.Chain;
        public X509Certificate2Collection Collection => _inner.Collection;
        public Identifier CommonName => _inner.CommonName;
        public AsymmetricKeyParameter? PrivateKey => _inner.PrivateKey;
        public IEnumerable<Identifier> SanNames => _inner.SanNames;
    }
}
