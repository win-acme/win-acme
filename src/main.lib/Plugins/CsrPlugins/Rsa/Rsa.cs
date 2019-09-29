using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Services;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class Rsa : CsrPlugin<Rsa, RsaOptions>
    {
        public Rsa(
            ILogService log,
            ISettingsService settings,
            PemService pemService,
            RsaOptions options) : base(log, settings, options, pemService) { }

        /// <summary>
        /// Generate new RSA key pair
        /// </summary>
        /// <returns></returns>
        internal override AsymmetricCipherKeyPair GenerateNewKeyPair()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var keyGenerationParameters = new KeyGenerationParameters(random, _settings.Security.RSAKeyBits);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            return subjectKeyPair;
        }

        /// <summary>
        /// Convert to Exchange format
        /// </summary>
        /// <param name="ackp"></param>
        /// <returns></returns>
        public override Task<X509Certificate2> PostProcess(X509Certificate2 original)
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
                var parameters = ((RSACng)original.PrivateKey).ExportParameters(true);
                rsaProvider.ImportParameters(parameters);
                var tempPfx = new X509Certificate2(original.Export(X509ContentType.Cert));
                tempPfx = tempPfx.CopyWithPrivateKey(rsaProvider);
                return Task.FromResult(tempPfx);
            }
            catch (Exception ex)
            {
                // If we couldn't convert the private key that 
                // means we're left with a pfx generated with the
                // 'wrong' Crypto provider therefor delete it to 
                // make sure it's retried on the next run.
                _log.Warning("Error converting private key to Microsoft RSA SChannel Cryptographic Provider, which means it might not be usable for Exchange 2013.");
                _log.Verbose("{ex}", ex);
                throw;
            }
        }

        public override string GetSignatureAlgorithm() => "SHA512withRSA";
    }
}
