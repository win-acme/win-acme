using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Extensions
{
    public static partial class X509Certificate2Extensions
    {
        public static List<Identifier> SanNames(this X509Certificate2 cert)
        {
            foreach (var x in cert.Extensions)
            {
                if ((x.Oid?.Value ?? "").Equals("2.5.29.17"))
                {
                    var result = ParseSubjectAlternativeNames(x.RawData);
                    return result.ToList();
                }
            }
            return new List<Identifier>();
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

        /// <summary>
        /// First part of the subject
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string? SubjectClean(this X509Certificate2 cert)
            => Split(cert.Subject) ?? "??";

        /// <summary>
        /// First part of the issuer
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static string? IssuerClean(this X509Certificate2 cert)
            => Split(cert.Issuer);

        /// <summary>
        /// Parse first part of distinguished name
        /// Format examples
        /// DNS Name=www.example.com
        /// DNS-имя=www.example.com
        /// CN=example.com, OU=Dept, O=Org 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string? Split(string input)
        {
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
