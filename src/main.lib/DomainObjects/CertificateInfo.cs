using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Provides information about a certificate, which may or may not already
    /// be stored on the disk somewhere in a .pfx file
    /// </summary>
    public class CertificateInfo
    {
        private const string SAN_OID = "2.5.29.17";

        public CertificateInfo(X509Certificate2Collection rawCollection)
        {
            Collection = rawCollection;

            var list = rawCollection.OfType<X509Certificate2>().ToList();
            // Get first certificate that has not been used to issue 
            // another one in the collection. That is the outermost leaf.
            var main = list.FirstOrDefault(x => !list.Any(y => x.Subject == y.Issuer));
            if (main == null)
            {
                // Self-signed (unit test)
                main = list.FirstOrDefault();
                if (main == null)
                {
                    throw new InvalidDataException("Empty X509Certificate2Collection");
                }
            }
            Certificate = main;

            list.Remove(main);
            var lastChainElement = main;
            var orderedCollection = new List<X509Certificate2>();
            while (list.Count > 0)
            {
                var signedBy = list.FirstOrDefault(x => lastChainElement.Issuer == x.Subject);
                if (signedBy == null)
                {
                    // Chain cannot be resolved any further
                    break;
                }
                orderedCollection.Add(signedBy);
                lastChainElement = signedBy;
                list.Remove(signedBy);
            }
            Chain = orderedCollection;
        }

        public X509Certificate2Collection Collection { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public List<X509Certificate2> Chain { get; set; }

        public FileInfo? CacheFile { get; set; }
        public string? CacheFilePassword { get; set; }

        public Identifier CommonName {
            get
            {
                var str = Certificate.SubjectClean();
                if (string.IsNullOrWhiteSpace(str))
                {
                    return SanNames.First();
                }
                return new DnsIdentifier(str);
            }
        }

        public Dictionary<Type, StoreInfo> StoreInfo { get; set; } = new Dictionary<Type, StoreInfo>();

        public List<Identifier> SanNames
        {
            get
            {
                foreach (var x in Certificate.Extensions)
                {
                    if ((x.Oid?.Value ?? "").Equals(SAN_OID))
                    {
                        var result = ParseSubjectAlternativeNames(x.RawData);
                        return result.ToList();
                    }
                }
                return new List<Identifier>();
            }
        }

        private static int ReadLength(ref Span<byte> span)
        {
            var length = (int)span[0];
            span = span[1..];
            if ((length & 0x80) > 0)
            {
                var lengthBytes = length & 0x7F;
                length = 0;
                for (var i = 0; i < lengthBytes; i++)
                {
                    length = (length * 0x100) + span[0];
                    span = span[1..];
                }
            }
            return length;
        }

        public static IList<Identifier> ParseSubjectAlternativeNames(byte[] rawData)
        {
            var result = new List<Identifier>(); // cannot yield results when using Span yet
            if (rawData.Length < 1 || rawData[0] != '0')
            {
                throw new InvalidDataException("They told me it will start with zero :(");
            }

            var data = rawData.AsSpan(1);
            var length = ReadLength(ref data);
            if (length != data.Length)
            {
                throw new InvalidDataException("I don't know who I am anymore");
            }

            while (!data.IsEmpty)
            {
                var type = data[0];
                data = data[1..];

                var partLength = ReadLength(ref data);
                if (type == 135) // ip
                {
                    result.Add(new IpIdentifier(new IPAddress(data[0..partLength]).ToString()));
                }
                else if (type == 160) // upn
                {
                    // not sure how to parse the part before \f
                    var index = data.IndexOf((byte)'\f') + 1;
                    var upnData = data[index..];
                    var upnLength = ReadLength(ref upnData);
                    result.Add(new UpnIdentifier(Encoding.UTF8.GetString(upnData[0..upnLength])));
                }
                else if (type == 130) // dns
                {
                    // IDN handling
                    var domainString = Encoding.UTF8.GetString(data[0..partLength]);
                    var idnIndex = domainString.IndexOf('(');
                    if (idnIndex > -1)
                    {
                        domainString = domainString[..idnIndex].Trim();
                    }
                    result.Add(new DnsIdentifier(domainString));
                }
                else // all other
                {
                    result.Add(new UnknownIdentifier(Encoding.UTF8.GetString(data[0..partLength])));
                }
                data = data[partLength..];
            }
            return result;
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
