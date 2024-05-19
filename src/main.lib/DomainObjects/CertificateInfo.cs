using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Provides information about a certificate, which may or may not already
    /// be stored on the disk somewhere in a .pfx file
    /// </summary>
    public partial class CertificateInfo : ICertificateInfo
    {

        private readonly byte[] _hash;
        private readonly List<Identifier> _san;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="store"></param>
        /// <exception cref="InvalidDataException"></exception>
        public CertificateInfo(Pkcs12Store store)
        {
            // Store original collection
            Collection = store;

            // Get first certificate that has not been used to issue 
            // another one in the collection. That is the outermost leaf
            // and thus will be our main certificate
            var certificates = store.
                Aliases.
                Select(alias => new { alias, store.GetCertificate(alias).Certificate }).
                ToList();
            if (certificates.Count == 0)
            {
                throw new InvalidDataException("Empty X509Certificate2Collection");
            }

            var main = certificates.FirstOrDefault(x => !certificates.Any(y => x.Certificate.SubjectDN.ToString() == y.Certificate.IssuerDN.ToString()));
           
            // Self-signed (unit test)
            main ??= certificates.First();

            Certificate = main.Certificate;
            FriendlyName = main.alias;

            // Compute fingerprint
            var encoded = Certificate.GetEncoded();
            var sha1 = new Sha1Digest();
            sha1.BlockUpdate(encoded, 0, encoded.Length);
            _hash = new byte[20];
            sha1.DoFinal(_hash, 0);
            Thumbprint = Convert.ToHexString(_hash);

            // Identify identifiers
            var str = Split(Certificate.SubjectDN?.ToString());
            if (!string.IsNullOrWhiteSpace(str))
            {
                CommonName = new DnsIdentifier(str);
            }
            _san = Certificate.
                GetSubjectAlternativeNameExtension().
                GetNames().
                Select<GeneralName, Identifier>(name =>
                {
                    var value = name.ToString().Split(": ")[1];
                    switch (name.TagNo)
                    {
                        case GeneralName.DnsName:
                            {
                                // IDN handling
                                var idnIndex = value.IndexOf('(');
                                if (idnIndex > -1)
                                {
                                    value = value[..idnIndex].Trim();
                                }
                                return new DnsIdentifier(value);
                            }
                        case GeneralName.IPAddress:
                            {
                                return new IpIdentifier(IPAddress.Parse(value.Replace("#", "0x")));
                            }
                        default:
                            {
                                return new UnknownIdentifier(value);
                            }
                    }
                }).ToList();

            // Check if we have the private key
            PrivateKey = store.
                Aliases.
                Where(store.IsKeyEntry).
                Select(a => store.GetKey(a).Key).
                FirstOrDefault();

            // Now order the remaining certificates in the correct order of who signed whom.
            var certonly = certificates.Select(t => t.Certificate).ToList();
            certonly.Remove(Certificate);
            var lastChainElement = Certificate;
            var orderedCollection = new List<X509Certificate>();
            while (certonly.Count > 0)
            {
                var signedBy = certonly.FirstOrDefault(x => lastChainElement.IssuerDN.ToString() == x.SubjectDN.ToString());
                if (signedBy == null)
                {
                    // Chain cannot be resolved any further
                    break;
                }
                orderedCollection.Add(signedBy);
                lastChainElement = signedBy;
                certonly.Remove(signedBy);
            }
            Chain = orderedCollection;
        }

        public Pkcs12Store Collection { get; private set; }

        public X509Certificate Certificate { get; private set; }

        public string FriendlyName { get; private set; }

        public AsymmetricKeyParameter? PrivateKey { get; private set; }

        public IEnumerable<X509Certificate> Chain { get; private set; }

        public Identifier? CommonName { get; private set; }

        public IEnumerable<Identifier> SanNames => _san;

        public byte[] GetHash() => _hash;

        public string Thumbprint { get; private set; }

        /// <summary>
        /// Parse first part of distinguished name
        /// Format examples
        /// DNS Name=www.example.com
        /// DNS-имя=www.example.com
        /// CN=example.com, OU=Dept, O=Org 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string? Split(string? input)
        {
            if (input == null)
            {
                return null;
            }
            var match = SplitRegex().Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            else
            {
                return null;
            }
        }

        [GeneratedRegex("=([^,]+)")]
        private static partial Regex SplitRegex();
    }
}
