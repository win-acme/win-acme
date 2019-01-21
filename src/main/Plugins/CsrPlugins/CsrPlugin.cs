using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.CsrPlugins
{
    /// <summary>
    /// Common implementation between RSA and EC certificates
    /// </summary>
    public abstract class CsrPlugin : ICsrPlugin
    {
        protected ILogService _log;
        public CsrPlugin(ILogService log)
        {
            _log = log;
        }

        public virtual bool CanConvert() => false;
        public virtual AsymmetricAlgorithm Convert(AsymmetricAlgorithm privateKey) => null;
        CertificateRequest ICsrPlugin.GenerateCsr(string commonName, List<string> identifiers)
        {
            var dn = CommonName(commonName, identifiers);
            var csr = GenerateCsr(dn);
            ProcessSan(identifiers, csr);
            return csr;
        }
        public abstract CertificateRequest GenerateCsr(X500DistinguishedName dn);
        public abstract AsymmetricKeyParameter GeneratePrivateKey();

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
