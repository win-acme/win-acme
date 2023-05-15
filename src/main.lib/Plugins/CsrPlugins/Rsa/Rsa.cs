using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    [IPlugin.Plugin<
        RsaOptions, CsrPluginOptionsFactory<RsaOptions>,
        DefaultCapability, WacsJsonPlugins>
        ("b9060d4b-c2d3-49ac-b37f-962e7c3cbe9d", 
        "RSA", "RSA key")]
    internal class Rsa : CsrPlugin<RsaOptions>
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

        public override string GetDefaultSignatureAlgorithm() => "SHA512withRSA";
    }
}
