using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
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

        public async Task<Target> Generate()
        {
            // Read CSR
            string csrString;
            if (string.IsNullOrEmpty(_options.CsrFile))
            {
                _log.Error("No CsrFile specified in options");
                return new NullTarget();
            }
            try
            {
                csrString = File.ReadAllText(_options.CsrFile);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to read CSR from {CsrFile}", _options.CsrFile);
                return new NullTarget();
            }

            // Parse CSR
            List<Identifier> alternativeNames;
            Identifier commonName;
            byte[] csrBytes;
            try
            {
                var pem = _pem.ParsePem<Pkcs10CertificationRequest>(csrString);
                if (pem == null)
                {
                    throw new Exception("Unable decode PEM bytes to Pkcs10CertificationRequest");
                }
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
                return new NullTarget();
            }

            AsymmetricKeyParameter? pkBytes = null;
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
                    return new NullTarget();
                }

                // Parse PK
                try
                {
                    var keyPair = _pem.ParsePem<AsymmetricCipherKeyPair>(pkString);

                    pkBytes = keyPair != null ? 
                        keyPair.Private : 
                        _pem.ParsePem<AsymmetricKeyParameter>(pkString);

                    if (pkBytes == null)
                    {
                        throw new Exception("No private key found");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to parse private key");
                    return new NullTarget();
                }
            }

            var ret = new Target($"[{nameof(Csr)}] {_options.CsrFile}",
                commonName,
                new List<TargetPart> {
                    new TargetPart(alternativeNames)
                })
            {
                CsrBytes = csrBytes,
                PrivateKey = pkBytes
            };
            return ret;
        }

        /// <summary>
        /// Get the common name
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private Identifier ParseCn(CertificationRequestInfo info)
        {
            var subject = info.Subject;
            var cnValue = subject.GetValueList(new DerObjectIdentifier("2.5.4.3"));
            if (cnValue.Count > 0)
            {
                var name = cnValue.Cast<string>().ElementAt(0);
                return new DnsIdentifier(name).Unicode(true);
            } 
            else
            {
                throw new Exception("Unable to parse common name");
            }
        }

        /// <summary>
        /// Parse the SAN names.
        /// Based on https://stackoverflow.com/questions/44824897/getting-subject-alternate-names-with-pkcs10certificationrequest
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private IEnumerable<Identifier> ParseSan(CertificationRequestInfo info)
        {
            var ret = new List<Identifier>();
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
            return names.GetNames().Select(x => x.TagNo switch {
                1 => new EmailIdentifier(x.Name.ToString()!),
                2 => new DnsIdentifier(x.Name.ToString()!).Unicode(true),
                7 => new IpIdentifier(x.Name.ToString()!),
                _ => new UnknownIdentifier(x.Name.ToString()!)
            });
        }

        private T? GetAsn1ObjectRecursive<T>(DerSequence sequence, string id) where T : Asn1Object
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

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}