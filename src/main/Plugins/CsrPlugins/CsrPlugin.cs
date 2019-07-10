using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
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

        public CsrPlugin(ILogService log, TOptions options)
        {
            _log = log;
            _options = options;
        }

        public virtual bool CanConvert() => false;
        public virtual AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey) => null;
        CertificateRequest ICsrPlugin.GenerateCsr(string cachePath, string commonName, List<string> identifiers)
        {
            FileInfo fi = null;
            if (!string.IsNullOrEmpty(cachePath))
            {
                try
                {
                    fi = new FileInfo(cachePath);
                    if (fi.Exists)
                    {
                        var rawData = new ProtectedString(File.ReadAllText(fi.FullName));
                        _cacheData = rawData.Value;
                        _log.Warning("Re-using key data generated at {time}", fi.LastWriteTime);
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

            var dn = CommonName(commonName, identifiers);
            var csr = GenerateCsr(dn);
            ProcessSan(identifiers, csr);
            if (_options.OcspMustStaple == true)
            {
                // OCSP Must-Staple
                _log.Information("Enable OCSP Must-Staple extension");
                csr.CertificateExtensions.Add(
                    new X509Extension("1.3.6.1.5.5.7.1.24",
                    new byte[] { 0x30, 0x03, 0x02, 0x01, 0x05 },
                    false));
            }

            if (fi != null && !string.IsNullOrEmpty(_cacheData))
            {
                var rawData = new ProtectedString(_cacheData);
                File.WriteAllText(fi.FullName, rawData.DiskValue);
            }

            return csr;
        }
        public abstract CertificateRequest GenerateCsr(X500DistinguishedName dn);
        public abstract AsymmetricKeyParameter GetPrivateKey();

        /// <summary>
        /// Determine the common name 
        /// </summary>
        /// <param name="commonName"></param>
        /// <param name="identifiers"></param>
        /// <returns></returns>
        public X500DistinguishedName CommonName(string commonName, List<string> identifiers)
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
            return new X500DistinguishedName($"CN={finalCommonName}");
        }

        /// <summary>
        /// Process the SAN extensions
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="request"></param>
        public void ProcessSan(List<string> identifiers, CertificateRequest request)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var n in identifiers)
            {
                sanBuilder.AddDnsName(n);
            }
            request.CertificateExtensions.Add(sanBuilder.Build());
        }
    }
}
