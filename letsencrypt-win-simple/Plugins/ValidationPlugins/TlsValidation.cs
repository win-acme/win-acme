using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class TlsValidation : IValidationPlugin
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_SNI;

        public abstract string Name { get; }
        public abstract string Description { get; }

        public Action<AuthorizationState> PrepareChallenge(Target target, AuthorizeChallenge challenge, string identifier, Options options, InputService input)
        {
            var tlsChallenge = challenge.Challenge as DnsChallenge;
            var token = tlsChallenge.Token;
            var answer = tlsChallenge.Answer as DnsChallengeAnswer;
            var tokenHash = GetHash(token);
            var answerHash = GetHash(answer.KeyAuthorization);

            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(serialNumber);
            certificateGenerator.SetNotBefore(DateTime.UtcNow);
            certificateGenerator.SetNotAfter(DateTime.Now.AddHours(1));

            var subjectDN = new X509Name("CN=letsencrypt");
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            var sanA = $"{tokenHash.Substring(0, 32)}.{tokenHash.Substring(32)}.token.acme.invalid";
            var sanB = $"{answerHash.Substring(0, 32)}.{answerHash.Substring(32)}.ka.acme.invalid";
            var subjectAlternativeNames = new Asn1Encodable[] {
                new GeneralName(GeneralName.DnsName, sanA),
                new GeneralName(GeneralName.DnsName, sanB)
            };
            var subjectAlternativeNamesExtension = new DerSequence(subjectAlternativeNames);
            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, subjectAlternativeNamesExtension);
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(new[] { new DerObjectIdentifier("1.3.6.1.5.5.7.3.1") }));
            var keyGenerationParameters = new KeyGenerationParameters(random, 2048);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var keyPair = keyPairGenerator.GenerateKeyPair();
            certificateGenerator.SetPublicKey(keyPair.Public);

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", keyPair.Private, random);

            var certificate = certificateGenerator.Generate(signatureFactory);
            var converted = DotNetUtilities.ToX509Certificate(certificate);

            // correcponding private key
            PrivateKeyInfo info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
            X509Certificate2 x509 = new X509Certificate2(certificate.GetEncoded());
            Asn1Sequence seq = (Asn1Sequence)Asn1Object.FromByteArray(info.ParsePrivateKey().GetDerEncoded());
            if (seq.Count != 9)
            {
                throw new Exception("malformed sequence in RSA private key");
            }
            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq); //new RsaPrivateKeyStructure(seq);
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, 
                rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, 
                rsa.Coefficient);
            x509.FriendlyName = "LetsEncrypt";
            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);

            InstallCertificate(target, identifier, x509);

            return authzState => RemoveCertificate(target, identifier, x509);
        }

        private string GetHash(string token)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            SHA256Managed algorithm = new SHA256Managed();
            byte[] hash = algorithm.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash) {
                hashString += string.Format("{0:x2}", x);
            }
            return hashString.ToLower();
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        public abstract void RemoveCertificate(Target target, string identifier, X509Certificate2 certificate);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        public abstract void InstallCertificate(Target target, string identifier, X509Certificate2 certificate);

        /// <summary>
        /// Should this validation option be shown for the target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool CanValidate(Target target)
        {
            return true;
        }

        public abstract void Aquire(Options options, InputService input, Target target);
        public abstract void Default(Options options, Target target);

        /// <summary>
        /// Create instance for specific target
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual IValidationPlugin CreateInstance(Target target)
        {
            return this;
        }

    }
}
