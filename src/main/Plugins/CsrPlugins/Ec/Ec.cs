using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Properties;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    internal class Ec : CsrPlugin<Ec, EcOptions>
    {
        public Ec(
            ILogService log,
            PemService pemService,
            EcOptions options) : base(log, options, pemService) { }

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
                var config = Settings.Default.ECCurve;
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

        public override string GetSignatureAlgorithm() => "SHA512withECDSA";
    }
}
