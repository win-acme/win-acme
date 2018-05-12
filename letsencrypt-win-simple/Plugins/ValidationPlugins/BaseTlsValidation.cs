using ACMESharp.ACME;
using PKISharp.WACS.Services;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PKISharp.WACS.Extensions;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for TLS-SNI-01 validation plugins
    /// </summary>
    internal abstract class BaseTlsValidation : BaseValidation<TlsSniChallenge>
    {
        protected ScheduledRenewal _renewal;
        private IEnumerable<CertificateInfo> _validationCertificates;

        public BaseTlsValidation(ILogService logService, ScheduledRenewal renewal, string identifier) :
            base(logService, identifier)
        {
            _renewal = renewal;
        }

        /// <summary>
        /// Handle the TlsSniChallenge
        /// </summary>
        public override void PrepareChallenge()
        {
            var answer = _challenge.Answer as TlsSniChallengeAnswer;
            _validationCertificates = GenerateCertificates(answer.KeyAuthorization, _challenge.IterationCount);
            foreach (var validationCertificate in _validationCertificates)
            {
                InstallCertificate(_renewal, validationCertificate);
            }
        }

        /// <summary>
        /// Delete certificates
        /// </summary>
        public override void CleanUp()
        {
            if (_validationCertificates != null)
            {
                foreach (var validationCertificate in _validationCertificates)
                {
                    RemoveCertificate(_renewal, validationCertificate);
                }
            }
        }

        /// <summary>
        /// Make certificate accessible for the world
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="certificateInfo"></param>
        public abstract void InstallCertificate(ScheduledRenewal renewal, CertificateInfo certificateInfo);

        /// <summary>
        /// Cleanup after validation
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="certificateInfo"></param>
        public abstract void RemoveCertificate(ScheduledRenewal renewal, CertificateInfo certificateInfo);

        /// <summary>
        /// Generate certificates according to documentation at
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.3
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="iterations">Number of certificates requested by challenge</param>
        /// <returns></returns>
        private IEnumerable<CertificateInfo> GenerateCertificates(string answer, int iterations)
        {
            var ret = new List<CertificateInfo>();
            var hash = answer;
            for (var i = 0; i < iterations; i++)
            {
                hash = hash.SHA256();
                var san = string.Empty;
                X509Certificate2 cert = null;
                do
                {
                    try
                    {
                        cert = GenerateCertificate(hash, out san);
                    }
                    catch (CryptographicException) { }
                } while (cert == null);
                ret.Add(new CertificateInfo() { Certificate = cert });
            }
            return ret;
        }

        /// <summary>
        /// Generate single certificate
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="san"></param>
        /// <returns></returns>
        private X509Certificate2 GenerateCertificate(string hash, out string san)
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetNotBefore(DateTime.UtcNow);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.AddHours(1));

            san = $"{hash.Substring(0, 32)}.{hash.Substring(32)}.acme.invalid";
            var subjectDN = new X509Name($"CN={san}");
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);
            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, 
                new DerSequence(new Asn1Encodable[] { new GeneralName(GeneralName.DnsName, san) }));
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, false, 
                new ExtendedKeyUsage(new KeyPurposeID[] { KeyPurposeID.IdKPServerAuth, KeyPurposeID.IdKPClientAuth }));
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, 
                new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var keyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(keyPair.Public);

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", keyPair.Private, random);
            var certificate = certificateGenerator.Generate(signatureFactory);
            var flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet;
            var x509 = new X509Certificate2(certificate.GetEncoded(), (string) null, flags)
            {
                FriendlyName = san,
                PrivateKey = ToDotNetKey((RsaPrivateCrtKeyParameters) keyPair.Private)
            };
            return x509;
        }

        /// <summary>
        /// Convert private key
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        private AsymmetricAlgorithm ToDotNetKey(RsaPrivateCrtKeyParameters privateKey)
        {
            var rsaProvider = new RSACryptoServiceProvider(new CspParameters {
                KeyContainerName = Guid.NewGuid().ToString(),
                KeyNumber = 1,
                Flags = CspProviderFlags.UseMachineKeyStore
            });
            var parameters = new RSAParameters {
                Modulus = privateKey.Modulus.ToByteArrayUnsigned(),
                P = privateKey.P.ToByteArrayUnsigned(),
                Q = privateKey.Q.ToByteArrayUnsigned(),
                DP = privateKey.DP.ToByteArrayUnsigned(),
                DQ = privateKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privateKey.QInv.ToByteArrayUnsigned(),
                D = privateKey.Exponent.ToByteArrayUnsigned(),
                Exponent = privateKey.PublicExponent.ToByteArrayUnsigned()
            };
            rsaProvider.ImportParameters(parameters);
            return rsaProvider;
        }
    }
}
