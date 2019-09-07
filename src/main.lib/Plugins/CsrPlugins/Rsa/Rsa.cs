using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Services;
using System;
using System.Security.Cryptography;

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
            var keyGenerationParameters = new KeyGenerationParameters(random, _settings.RSAKeyBits);
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
        public override AsymmetricAlgorithm Convert(AsymmetricAlgorithm ackp)
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

        public override bool CanConvert() => true;

        public override string GetSignatureAlgorithm() => "SHA512withRSA";
    }
}
