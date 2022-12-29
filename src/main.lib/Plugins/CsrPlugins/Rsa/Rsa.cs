using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    [IPlugin.Plugin<RsaOptions, RsaOptionsFactory, WacsJsonPlugins>
        ("b9060d4b-c2d3-49ac-b37f-962e7c3cbe9d", "RSA", "RSA key")]
    internal class Rsa : CsrPlugin<Rsa, RsaOptions>
    {
        private static readonly object PostProcessLock = new();

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
        [SupportedOSPlatform("windows")]
        public override Task<X509Certificate2> PostProcess(X509Certificate2 original)
        {
            // Lock this section of code because we have seen
            // exceptions and errors in heavy threading scenarios,
            // which might be an underlying framework/OS bug
            lock (PostProcessLock) {
                using var privateKey = original.GetRSAPrivateKey();
                if (privateKey == null)
                {
                    return Task.FromResult(original);
                }

                // https://github.com/dotnet/runtime/issues/36899
                var pwd = Guid.NewGuid().ToString();
                using var tempRsa = RSA.Create();
                var pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 10);
                tempRsa.ImportEncryptedPkcs8PrivateKey(pwd, privateKey.ExportEncryptedPkcs8PrivateKey(pwd, pbeParameters), out var read);

                try
                {
                    var cspParameters = new CspParameters
                    {
                        KeyContainerName = Guid.NewGuid().ToString(),
                        KeyNumber = 1,
                        Flags = CspProviderFlags.NoPrompt,
                        ProviderType = 12 // Microsoft RSA SChannel Cryptographic Provider
                    };
                    var rsaProvider = new RSACryptoServiceProvider(cspParameters);
                    var parameters = tempRsa.ExportParameters(true);
                    rsaProvider.ImportParameters(parameters);

                    var tempPfx = new X509Certificate2(
                        original.Export(X509ContentType.Cert),
                        "",
                        X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
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
        }

        public override string GetSignatureAlgorithm() => "SHA512withRSA";
    }
}
