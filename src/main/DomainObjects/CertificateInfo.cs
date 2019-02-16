using PKISharp.WACS.Extensions;
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
        public X509Certificate2 Certificate { get; set; }
        public X509Store Store { get; set; }
        public FileInfo PfxFile { get; set; }
        public string PfxFilePassword { get; set; }
        public string SubjectName => Certificate.Subject.Replace("CN=", "").Trim();

        public List<string> HostNames
        {
            get
            {
                var ret = new List<string>();
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

        /// <summary>
        /// See if the information in the certificate matches
        /// that of the specified target. Used to figure out whether
        /// or not the cache is out of date.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool Match(Target target)
        {
            var identifiers = target.GetHosts(false);
            var idn = new IdnMapping();
            return SubjectName == target.CommonName &&
                HostNames.Count == identifiers.Count() &&
                HostNames.All(h => identifiers.Contains(idn.GetAscii(h)));
        }
    }
}
