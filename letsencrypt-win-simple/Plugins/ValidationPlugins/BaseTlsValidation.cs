using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
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

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    internal abstract class BaseTlsValidation : IValidationPlugin
    {
        protected ScheduledRenewal _renewal;
     
        public BaseTlsValidation(ScheduledRenewal renewal)
        {
            _renewal = renewal;
        }

        public Action<AuthorizationState> PrepareChallenge(AuthorizeChallenge challenge, string identifier)
        {
            TlsSniChallenge tlsChallenge = challenge.Challenge as TlsSniChallenge;
            TlsSniChallengeAnswer answer = tlsChallenge.Answer as TlsSniChallengeAnswer;
            IEnumerable<CertificateInfo> validationCertificates = GenerateCertificates(answer.KeyAuthorization, tlsChallenge.IterationCount);
            foreach (var validationCertificate in validationCertificates)
            {
                InstallCertificate(_renewal, validationCertificate);
            }
            return (AuthorizationState authzState) => {
                foreach (var validationCertificate in validationCertificates)
                {
                    RemoveCertificate(_renewal, validationCertificate);
                }
            };
        }

        public abstract void InstallCertificate(ScheduledRenewal renewal, CertificateInfo certificateInfo);
        public abstract void RemoveCertificate(ScheduledRenewal renewal, CertificateInfo certificateInfo);

        /// <summary>
        /// Generate certificate according to documentation at
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.3
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="san"></param>
        /// <returns></returns>
        private IEnumerable<CertificateInfo> GenerateCertificates(string answer, int iterations)
        {
            var ret = new List<CertificateInfo>();
            string hash = answer;
            for (var i = 0; i < iterations; i++)
            {
                hash = GetHash(hash);
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

        private X509Certificate2 GenerateCertificate(string hash, out string san)
        {
            CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(randomGenerator);
            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetNotBefore(DateTime.UtcNow);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.AddHours(1));

            san = string.Format("{0}.{1}.acme.invalid", hash.Substring(0, 32), hash.Substring(32));
            X509Name subjectDN = new X509Name(string.Format("CN={0}", san));
            X509Name issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);
            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, 
                new DerSequence(new Asn1Encodable[] { new GeneralName(GeneralName.DnsName, san) }));
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, false, 
                new ExtendedKeyUsage(new KeyPurposeID[] { KeyPurposeID.IdKPServerAuth, KeyPurposeID.IdKPClientAuth }));
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, 
                new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));
            KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(keyPair.Public);

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", keyPair.Private, random);
            Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);
            var flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet;
            var x509 = new X509Certificate2(certificate.GetEncoded(), (string)null, flags);
            x509.FriendlyName = san;
            x509.PrivateKey = ToDotNetKey((RsaPrivateCrtKeyParameters)keyPair.Private);
            return x509;
        }

        private AsymmetricAlgorithm ToDotNetKey(RsaPrivateCrtKeyParameters privateKey)
        {
            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(new CspParameters {
                KeyContainerName = Guid.NewGuid().ToString(),
                KeyNumber = 1,
                Flags = CspProviderFlags.UseMachineKeyStore
            });
            RSAParameters parameters = new RSAParameters {
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

        private string GetHash(string token)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            SHA256Managed algorithm = new SHA256Managed();
            byte[] hash = algorithm.ComputeHash(bytes);
            string hashString = string.Empty;
            byte[] array = hash;
            for (int i = 0; i < array.Length; i++)
            {
                byte x = array[i];
                hashString += string.Format("{0:x2}", x);
            }
            return hashString.ToLower();
        }
    }
}
