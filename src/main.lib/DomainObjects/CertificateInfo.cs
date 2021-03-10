using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Provides information about a certificate, which may or may not already
    /// be stored on the disk somewhere in a .pfx file
    /// </summary>
    public class CertificateInfo
    {
        public CertificateInfo(X509Certificate2 certificate) => Certificate = certificate;

        public X509Certificate2 Certificate { get; set; }
        public List<X509Certificate2> Chain { get; set; } = new List<X509Certificate2>();
        public FileInfo? CacheFile { get; set; }
        public string? CacheFilePassword { get; set; }
        public string CommonName => Split(Certificate.Subject) ?? SanNames.First();
        public string Issuer => Split(Certificate.Issuer) ?? "??";

        /// <summary>
        /// Parse first part of distinguished name
        /// Format examples
        /// DNS Name=www.example.com
        /// DNS-имя=www.example.com
        /// CN=example.com, OU=Dept, O=Org 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string? Split(string input)
        {
            var match = Regex.Match(input, "=([^,]+)");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            } 
            else
            {
                return null;
            }
        }

        public Dictionary<Type, StoreInfo> StoreInfo { get; set; } = new Dictionary<Type, StoreInfo>();

        public List<string> SanNames
        {
            get
            {
                var ret = new List<string>();
                foreach (var x in Certificate.Extensions)
                {
                    if ((x.Oid?.Value ?? "").Equals("2.5.29.17"))
                    {
                        var asndata = new AsnEncodedData(x.Oid, x.RawData);
                        var parts = asndata.Format(true).Trim().Split('\n');
                        foreach (var part in parts)
                        {
                            var domainString = Split(part);
                            if (domainString != null)
                            {
                                // IDN handling
                                var idnIndex = domainString.IndexOf('(');
                                if (idnIndex > -1)
                                {
                                    domainString = domainString.Substring(0, idnIndex).Trim();
                                }
                                ret.Add(domainString);
                            }

                        }
                    }
                }
                return ret;
            }
        }
    }

    /// <summary>
    /// Information about where the certificate is stored
    /// </summary>
    public class StoreInfo
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
    }
}
