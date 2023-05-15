using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    [IPlugin.Plugin<
        EcOptions, CsrPluginOptionsFactory<EcOptions>, 
        DefaultCapability, WacsJsonPlugins>
        ("9aadcf71-5241-4c4f-aee1-bfe3f6be3489", 
        "EC", "Elliptic Curve key")]
    internal class Ec : CsrPlugin<EcOptions>
    {
        public Ec(
            ILogService log,
            ISettingsService settings,
            PemService pemService,
            EcOptions options) : base(log, settings, options, pemService) { }

        internal override AsymmetricCipherKeyPair GenerateNewKeyPair()
        {
            var generator = new ECKeyPairGenerator();
            var curve = GetEcCurve();
            var genParam = new ECKeyGenerationParameters(
                SecNamedCurves.GetOid(curve),
                new SecureRandom());
            generator.Init(genParam);
            return generator.GenerateKeyPair();
        }

        /// <summary>
        /// Parameters to generate the key for
        /// </summary>
        /// <returns></returns>
        private string GetEcCurve()
        {
            var ret = "secp384r1"; // Default
            try
            {
                var config = _settings.Security.ECCurve;
                if (config != null)
                {
                    DerObjectIdentifier? curveOid = null;
                    try
                    {
                        curveOid = SecNamedCurves.GetOid(config);
                    }
                    catch { }
                    if (curveOid != null)
                    {
                        ret = config;
                    }
                    else
                    {
                        _log.Warning("Unknown curve {ECCurve}", config);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to get EC name, error: {@ex}", ex);
            }
            _log.Debug("ECCurve: {ECCurve}", ret);
            return ret;
        }

        public override string GetDefaultSignatureAlgorithm() => "SHA512withECDSA";
    }
}
