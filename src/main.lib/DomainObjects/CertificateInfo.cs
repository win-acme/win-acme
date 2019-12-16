using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
        public string SubjectName => Certificate.Subject.Replace("CN=", "").Trim();
     
        public Dictionary<Type, StoreInfo> StoreInfo { get; set; } = new Dictionary<Type, StoreInfo>();

        public List<string> HostNames
        {
            get
            {
                var ret = new List<string>();
                if (Certificate == null)
                {
                    return ret;
                }
                foreach (var x in Certificate.Extensions)
                {
                    if (x.Oid.Value.Equals("2.5.29.17"))
                    {
                        var asndata = new AsnEncodedData(x.Oid, x.RawData);
                        var parts = asndata.Format(true).Trim().Split('\n');
                        foreach (var part in parts)
                        {
                            // Format DNS Name=www.example.com
                            // but on localized OS can also be DNS-имя=www.example.com
                            var domainString = part.Split('=')[1].Trim();
                            // IDN
                            var idnIndex = domainString.IndexOf('(');
                            if (idnIndex > -1)
                            {
                                domainString = domainString.Substring(0, idnIndex).Trim();
                            }
                            ret.Add(domainString);
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
