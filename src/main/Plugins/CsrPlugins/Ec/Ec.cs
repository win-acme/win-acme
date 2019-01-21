using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class Ec : CsrPlugin
    {
        public Ec(ILogService log) : base(log) { }

        public override CertificateRequest GenerateCsr(X500DistinguishedName commonName)
        {
            return new CertificateRequest(commonName, Algorithm, HashAlgorithmName.SHA256);
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
        public override AsymmetricKeyParameter GeneratePrivateKey()
        {
            if (_keyPair == null)
            {
                var generator = new ECKeyPairGenerator();
                var curve = GetEcCurve();
                ECKeyGenerationParameters genParam = new ECKeyGenerationParameters(
                    SecNamedCurves.GetOid(curve),
                    new SecureRandom());
                generator.Init(genParam);
                _keyPair = generator.GenerateKeyPair();
            }
            return _keyPair.Private;
        }
        private AsymmetricCipherKeyPair _keyPair;

        /// <summary>
        /// Parameters to generate the key for
        /// </summary>
        /// <returns></returns>
        private string GetEcCurve()
        {
            var ret = "secp384r1"; // Default
            try
            {
                var config = Properties.Settings.Default.ECCurve;
                DerObjectIdentifier curveOid = null;
                try
                {
                    curveOid = SecNamedCurves.GetOid(config);
                }
                catch {}
                if (curveOid != null)
                {
                    ret = config;
                }
                else
                {
                    _log.Warning("Unknown curve {ECCurve}", config);
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to get EC name, error: {@ex}", ex);
            }
            _log.Debug("ECCurve: {ECCurve}", ret);
            return ret;
        }

    }
}
