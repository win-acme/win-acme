using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class Ec : ICsrPlugin
    {
        private ILogService _log;
        public AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey) => null;
        public bool CanConvert() => false;

        public Ec(ILogService log)
        {
            _log = log;
        }

        public CertificateRequest GenerateCsr(string commonName, List<string> identifiers)
        {
            var idn = new IdnMapping();
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = idn.GetAscii(commonName);
                if (!identifiers.Contains(commonName, StringComparer.InvariantCultureIgnoreCase))
                {
                    _log.Warning($"Common name {commonName} provided is invalid.");
                    commonName = null;
                }
            }
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var n in identifiers)
            {
                sanBuilder.AddDnsName(n);
            }
            var finalCommonName = commonName ?? identifiers.FirstOrDefault();
            var dn = new X500DistinguishedName($"CN={finalCommonName}");
            var csr = new CertificateRequest(dn, Algorithm, HashAlgorithmName.SHA256);
            csr.CertificateExtensions.Add(sanBuilder.Build());
            return csr;
        }

        /// <summary>
        /// Create or return algorithm
        /// </summary>
        private ECDsaCng Algorithm
        {
            get
            {
                if (_algorithm == null)
                {
                    var bcKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(GeneratePrivateKey());
                    var pkcs8Blob = bcKeyInfo.GetDerEncoded();
                    var importedKey = CngKey.Import(pkcs8Blob, CngKeyBlobFormat.Pkcs8PrivateBlob);
                    _algorithm = new ECDsaCng(importedKey);

                }
                return _algorithm;
            }
        }
        private ECDsaCng _algorithm;

        /// <summary>
        /// Generate or return private key
        /// </summary>
        /// <returns></returns>
        public AsymmetricKeyParameter GeneratePrivateKey()
        {
            if (_keyPair == null)
            {
                var generator = new ECKeyPairGenerator();
                ECKeyGenerationParameters genParam = new ECKeyGenerationParameters(
                    SecNamedCurves.GetOid("secp384r1"),
                    new SecureRandom());
                generator.Init(genParam);
                _keyPair = generator.GenerateKeyPair();
            }
            return _keyPair.Private;
        }
        private AsymmetricCipherKeyPair _keyPair;
    }
}
