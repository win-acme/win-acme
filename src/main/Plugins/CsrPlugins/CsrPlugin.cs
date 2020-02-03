using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    /// <summary>
    /// Common implementation between RSA and EC certificates
    /// </summary>
    public abstract class CsrPlugin<TPlugin, TOptions> : ICsrPlugin
        where TOptions : CsrPluginOptions<TPlugin>
        where TPlugin : ICsrPlugin
    {
        protected ILogService _log;
        protected TOptions _options;
        protected string _cacheData;
        private AsymmetricCipherKeyPair _keyPair;
        private readonly PemService _pemService;

        public CsrPlugin(ILogService log, TOptions options, PemService pemService)
        {
            _log = log;
            _options = options;
            _pemService = pemService;
        }

        public virtual bool CanConvert() => false;
        public virtual AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey) => null;
        Pkcs10CertificationRequest ICsrPlugin.GenerateCsr(string cachePath, string commonName, List<string> identifiers)
        {
            var extensions = new Dictionary<DerObjectIdentifier, X509Extension>();

            LoadFromCache(cachePath);

            var dn = CommonName(commonName, identifiers);
            var keys = GetKeys();

            ProcessMustStaple(extensions);
            ProcessSan(identifiers, extensions);

            var csr = new Pkcs10CertificationRequest(
                new Asn1SignatureFactory(GetSignatureAlgorithm(), keys.Private),
                dn,
                keys.Public,
                new DerSet(new AttributePkcs(
                    PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                    new DerSet(new X509Extensions(extensions)))));

            SaveToCache(cachePath);
            return csr;
        }

        public abstract string GetSignatureAlgorithm();
        /// <summary>
        /// Generate new public/private key pair
        /// </summary>
        /// <returns></returns>
        internal abstract AsymmetricCipherKeyPair GenerateNewKeyPair();

        /// <summary>
        /// Get public and private keys
        /// </summary>
        /// <returns></returns>
        public AsymmetricCipherKeyPair GetKeys()
        {
            if (_keyPair == null)
            {
                if (_cacheData == null)
                {
                    _keyPair = GenerateNewKeyPair();
                    _cacheData = _pemService.GetPem(_keyPair);
                }
                else
                {
                    try
                    {
                        _keyPair = _pemService.ParsePem<AsymmetricCipherKeyPair>(_cacheData);
                        if (_keyPair == null)
                        {
                            throw new InvalidDataException("key");
                        }
                    }
                    catch
                    {
                        _log.Error($"Unable to read cache data, creating new key...");
                        _cacheData = null;
                        return GetKeys();
                    }
                }
            }
            return _keyPair;
        }

        /// <summary>
        /// Load cached key information from disk, if needed
        /// </summary>
        /// <param name="cachePath"></param>
        private void LoadFromCache(string cachePath)
        {
            if (_options.ReusePrivateKey == true)
            {
                try
                {
                    var fi = new FileInfo(cachePath);
                    if (fi.Exists)
                    {
                        var rawData = new ProtectedString(File.ReadAllText(cachePath), _log);
                        if (!rawData.Error)
                        {
                            _cacheData = rawData.Value;
                            _log.Warning("Re-using key data generated at {time}", fi.LastWriteTime);
                        }
                        else
                        {
                            _log.Warning("Key reuse is enabled but file {cachePath} cannot be decrypted, creating new key...", cachePath);
                        }
                    }
                    else
                    {
                        _log.Warning("Key reuse is enabled but file {cachePath} does't exist yet, creating new key...", cachePath);
                    }
                }
                catch
                {
                    throw new Exception($"Unable to read from cache file {cachePath}");
                }
            }
        }

        /// <summary>
        /// Save cached key information to disk, if needed
        /// </summary>
        /// <param name="cachePath"></param>
        private void SaveToCache(string cachePath)
        {
            if (_options.ReusePrivateKey == true)
            {
                var rawData = new ProtectedString(_cacheData);
                File.WriteAllText(cachePath, rawData.DiskValue);
            }
        }

        /// <summary>
        /// Optionally add the OCSP Must-Stable extension
        /// </summary>
        /// <param name="extensions"></param>
        private void ProcessMustStaple(Dictionary<DerObjectIdentifier, X509Extension> extensions)
        {
            // OCSP Must-Staple
            if (_options.OcspMustStaple == true)
            {

                _log.Information("Enable OCSP Must-Staple extension");
                extensions.Add(
                    new DerObjectIdentifier("1.3.6.1.5.5.7.1.24"),
                    new X509Extension(
                        false,
                        new DerOctetString(new byte[]
                        {
                            0x30, 0x03, 0x02, 0x01, 0x05
                        })));
            }
        }

        /// <summary>
        /// Determine the common name 
        /// </summary>
        /// <param name="commonName"></param>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        public X509Name CommonName(string commonName, List<string> identifiers)
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
            var finalCommonName = commonName ?? identifiers.FirstOrDefault();
            IDictionary attrs = new Hashtable
            {
                [X509Name.CN] = finalCommonName
            };
            IList ord = new ArrayList
            {
                X509Name.CN
            };
            var issuerDN = new X509Name(ord, attrs);
            return issuerDN;
        }

        /// <summary>
        /// Process the SAN extensions
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="request"></param>
        public void ProcessSan(List<string> identifiers, Dictionary<DerObjectIdentifier, X509Extension> extensions)
        {
            // SAN
            var names = new GeneralNames(identifiers.
                Select(n => new GeneralName(GeneralName.DnsName, n)).
                ToArray());
            Asn1OctetString asn1ost = new DerOctetString(names);
            extensions.Add(X509Extensions.SubjectAlternativeName, new X509Extension(false, asn1ost));
        }
    }
}
