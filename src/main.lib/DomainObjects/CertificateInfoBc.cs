using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bc = Org.BouncyCastle;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Special implementation of CertificateInfo which contains reference
    /// to a file in the cache
    /// </summary>
    public class CertificateInfoBc : ICertificateInfo
    {
        private readonly CertificateInfo _inner;

        /// <summary>
        /// Contruction with two attempts: as EphemeralKeySet first 
        /// and fallback to MachineKeySet second for missing/corrupt
        /// profiles
        /// </summary>
        /// <param name="file"></param>
        /// <param name="password"></param>
        public CertificateInfoBc(Pkcs12Store store)
        {
            try
            {
                _inner = GenerateInner(store, X509KeyStorageFlags.EphemeralKeySet);
            }
            catch (CryptographicException)
            {
                _inner = GenerateInner(store, X509KeyStorageFlags.MachineKeySet);
            }
        }

        /// <summary>
        /// Convert BouncyCastle Pkcs12Store to .NET X509Certificate2Collection
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        private CertificateInfo GenerateInner(Pkcs12Store store, X509KeyStorageFlags flags)
        {
            var tempPassword = PasswordGenerator.Generate();
            var pfxStream = new MemoryStream();
            store.Save(pfxStream, tempPassword.ToCharArray(), new Bc.Security.SecureRandom());
            pfxStream.Position = 0;

            var finalFlags = flags |= X509KeyStorageFlags.Exportable;

            using var pfxStreamReader = new BinaryReader(pfxStream);
            var tempPfx = new X509Certificate2Collection();
            tempPfx.Import(
                pfxStreamReader.ReadBytes((int)pfxStream.Length),
                tempPassword,
                finalFlags);
            return new CertificateInfo(tempPfx);
        }

        public X509Certificate2 Certificate => _inner.Certificate;
        public IEnumerable<X509Certificate2> Chain => _inner.Chain;
        public X509Certificate2Collection Collection => _inner.Collection;
        public Identifier? CommonName => _inner.CommonName;
        public AsymmetricKeyParameter? PrivateKey => _inner.PrivateKey;
        public IEnumerable<Identifier> SanNames => _inner.SanNames;
    }
}
