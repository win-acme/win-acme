using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class Csr : ITargetPlugin
    {
        private readonly ILogService _log;
        private readonly PemService _pem;
        private readonly CsrOptions _options;

        public Csr(ILogService logService, PemService pemService, CsrOptions options)
        {
            _log = logService;
            _pem = pemService;
            _options = options;
        }

        public Task<Target> Generate()
        {
            // Read CSR
            string csrString;
            try
            {
                csrString = File.ReadAllText(_options.CsrFile);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to read CSR from {CsrFile}", _options.CsrFile);
                return Task.FromResult<Target>(null);
            }

            // Parse CSR
            List<string> alternativeNames;
            string commonName;
            byte[] csrBytes;
            try
            {
                var pem = _pem.ParsePem<Pkcs10CertificationRequest>(csrString);
                var info = pem.GetCertificationRequestInfo();
                csrBytes = pem.GetEncoded();
                commonName = ParseCn(info);
                alternativeNames = ParseSan(info).ToList();
                if (!alternativeNames.Contains(commonName))
                {
                    alternativeNames.Add(commonName);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to parse CSR");
                return Task.FromResult<Target>(null);
            }

            AsymmetricKeyParameter pkBytes = null;
            if (!string.IsNullOrWhiteSpace(_options.PkFile))
            {
                // Read PK
                string pkString;
                try
                {
                    pkString = File.ReadAllText(_options.PkFile);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to read private key from {PkFile}", _options.PkFile);
                    return null;
                }

                // Parse PK
                try
                {
                    pkBytes = _pem.ParsePem<AsymmetricKeyParameter>(pkString);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to parse private key");
                    return null;
                }
            }

            return Task.FromResult(new Target()
            {
                FriendlyName = $"[{nameof(Csr)}] {_options.CsrFile}",
                CommonName = commonName,
                Parts = new List<TargetPart> {
                    new TargetPart {
                        Identifiers = alternativeNames
                    }
                },
                CsrBytes = csrBytes,
                PrivateKey = pkBytes
            });
        }

        /// <summary>
        /// Get the common name
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private string ParseCn(CertificationRequestInfo info)
        {
            var subject = info.Subject;
            var cnValue = (ArrayList)subject.GetValueList(new DerObjectIdentifier("2.5.4.3"));
            return ProcessName((string)cnValue[0]);
        }

        /// <summary>
        /// Convert puny-code to unicode
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string ProcessName(string name)
        {
            var idn = new IdnMapping();
            return idn.GetUnicode(name.ToLower());
        }

        /// <summary>
        /// Parse the SAN names.
        /// Based on https://stackoverflow.com/questions/44824897/getting-subject-alternate-names-with-pkcs10certificationrequest
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private IEnumerable<string> ParseSan(CertificationRequestInfo info)
        {
            var ret = new List<string>();
            var extensionSequence = info.Attributes.OfType<DerSequence>()
                .Where(o => o.OfType<DerObjectIdentifier>().Any(oo => oo.Id == "1.2.840.113549.1.9.14"))
                .FirstOrDefault();
            if (extensionSequence == null)
            {
                return ret;
            }
            var extensionSet = extensionSequence.OfType<DerSet>().FirstOrDefault();
            if (extensionSet == null)
            {
                return ret;
            }
            var sequence = extensionSet.OfType<DerSequence>().FirstOrDefault();
            if (sequence == null)
            {
                return ret;
            }
            var derOctetString = GetAsn1ObjectRecursive<DerOctetString>(sequence, "2.5.29.17");
            if (derOctetString == null)
            {
                return ret;
            }
            var asn1object = Asn1Object.FromByteArray(derOctetString.GetOctets());
            var names = Org.BouncyCastle.Asn1.X509.GeneralNames.GetInstance(asn1object);
            return names.GetNames().Select(x => ProcessName(x.Name.ToString()));
        }

        private T GetAsn1ObjectRecursive<T>(DerSequence sequence, string id) where T : Asn1Object
        {
            if (sequence.OfType<DerObjectIdentifier>().Any(o => o.Id == id))
            {
                return sequence.OfType<T>().First();
            }
            foreach (var subSequence in sequence.OfType<DerSequence>())
            {
                var value = GetAsn1ObjectRecursive<T>(subSequence, id);
                if (value != default(T))
                {
                    return value;
                }
            }
            return default;
        }

        public bool Disabled => false;
    }
}