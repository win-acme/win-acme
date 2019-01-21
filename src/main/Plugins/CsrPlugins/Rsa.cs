using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.Crypto;
using Org.BouncyCastle.Crypto;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using bc = Org.BouncyCastle;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    class Rsa : ICsrPlugin
    {
        private ILogService _log;

        public Rsa(ILogService log)
        {
            _log = log;
        }

        /// <summary>
        /// Generate CSR
        /// </summary>
        /// <param name="commonName"></param>
        /// <param name="identifiers"></param>
        /// <returns></returns>
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
            var csr = new CertificateRequest(dn, Algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            csr.CertificateExtensions.Add(sanBuilder.Build());
            return csr;
        }

        /// <summary>
        /// Parameters to generate the key for
        /// </summary>
        /// <returns></returns>
        private int GetRsaKeyBits()
        {
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 2048)
                {
                    _log.Debug("RSAKeyBits: {RSAKeyBits}", Properties.Settings.Default.RSAKeyBits);
                    return Properties.Settings.Default.RSAKeyBits;
                }
                else
                {
                    _log.Warning("RSA key bits less than 2048 is not secure.");
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to get RSA Key bits, error: {@ex}", ex);
            }
            return 2048;
        }

        /// <summary>
        /// Create or return algorithm
        /// </summary>
        private RSA Algorithm
        {
            get
            {
                if (_algorithm == null)
                {
                    var serializedKeys = CryptoHelper.Rsa.GenerateKeys(RSA.Create(GetRsaKeyBits()));
                    _algorithm = CryptoHelper.Rsa.GenerateAlgorithm(serializedKeys);
                }
                return _algorithm;
            }
        }
        private RSA _algorithm;

        /// <summary>
        /// Generate or return private key
        /// </summary>
        /// <returns></returns>
        public AsymmetricKeyParameter GeneratePrivateKey()
        {
            var keyParams = bc.Security.DotNetUtilities.GetRsaKeyPair(Algorithm.ExportParameters(true));
            return keyParams.Private;
        }

        /// <summary>
        /// Convert to Exchange format
        /// </summary>
        /// <param name="ackp"></param>
        /// <returns></returns>
        public AsymmetricAlgorithm Convert(AsymmetricAlgorithm ackp)
        {
            try
            {
                var cspParameters = new CspParameters
                {
                    KeyContainerName = Guid.NewGuid().ToString(),
                    KeyNumber = 1,
                    Flags = CspProviderFlags.UseMachineKeyStore,
                    ProviderType = 12 // Microsoft RSA SChannel Cryptographic Provider
                };
                var rsaProvider = new RSACryptoServiceProvider(cspParameters);
                var parameters = ((RSACryptoServiceProvider)ackp).ExportParameters(true);
                rsaProvider.ImportParameters(parameters);
                return rsaProvider;
            }
            catch (Exception ex)
            {
                // If we couldn't convert the private key that 
                // means we're left with a pfx generated with the
                // 'wrong' Crypto provider therefor delete it to 
                // make sure it's retried on the next run.
                _log.Warning("Error converting private key to Microsoft RSA SChannel Cryptographic Provider, which means it might not be usable for Exchange.");
                _log.Verbose("{ex}", ex);
                throw;
            }
        }

        public bool CanConvert() => true;
    }
}
