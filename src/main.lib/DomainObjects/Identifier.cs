using MimeKit;
using PKISharp.WACS.Extensions;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.DomainObjects
{
    public enum IdentifierType
    {
        Unknown,
        IpAddress,
        DnsName,
        UpnName,
        Email
    }

    [DebuggerDisplay("{Type}: {Value}")]
    public abstract class Identifier : IEquatable<Identifier>, IComparable, IComparable<Identifier>
    {
        public Identifier(string value, IdentifierType identifierType = IdentifierType.Unknown)
        {
            Value = value;
            Type = identifierType;
        }

        public virtual Identifier Unicode(bool unicode) => this;

        public IdentifierType Type { get; set; }
        public string Value { get; set; }
        public override string ToString() => $"{Type}: {Value}";
        public override bool Equals(object? obj) => (obj as Identifier) == this;
        public override int GetHashCode() => ToString().GetHashCode();
        public bool Equals(Identifier? other) => other == this;
        public int CompareTo(object? obj) => ToString().CompareTo((obj as Identifier)?.ToString());
        public int CompareTo(Identifier? other) => ToString().CompareTo(other?.ToString());
        public static bool operator ==(Identifier? a, Identifier? b) => string.Equals(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
        public static bool operator !=(Identifier? a, Identifier? b) => !(a == b);
    }

    public class DnsIdentifier : Identifier
    {
        public DnsIdentifier(string value) : base(value, IdentifierType.DnsName)
        {

        }

        public override Identifier Unicode(bool unicode)
        {
            if (unicode)
            {
                return new DnsIdentifier(Value.ConvertPunycode());
            }
            else
            {
                var idn = new IdnMapping();
                return new DnsIdentifier(idn.GetAscii(Value));
            }
        }
    }

    public class IpIdentifier : Identifier
    {
        public IpIdentifier(IPAddress value) : base(value.ToString(), IdentifierType.IpAddress) {}

        public IpIdentifier(string value) : base(value, IdentifierType.IpAddress)
        {
            if (IPAddress.TryParse(value, out var parsed))
            {
                Value = parsed.ToString();
                return;
            }
            if (value.StartsWith("#"))
            {
                var hex = value.TrimStart('#');
                try
                {
                    var bytes = Enumerable.Range(0, hex.Length / 2).Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16)).ToArray();
                    var ip = new IPAddress(bytes);
                    Value = ip.ToString();
                    return;
                }
                catch {}
            }
            throw new ArgumentException("Value is not recognized as a valid IP address");
        }
    }
    public class EmailIdentifier : Identifier
    {
        public EmailIdentifier(string value) : base(value, IdentifierType.Email)
        {
            try
            {
                var sender = new MailboxAddress("Test", value);
            } 
            catch
            {
                throw new ArgumentException("Value is not recognized as a valid email address");
            }
        }
    }

    public class UpnIdentifier : Identifier
    {
        public UpnIdentifier(string value) : base(value, IdentifierType.UpnName)
        {

        }
    }

    public class UnknownIdentifier : Identifier
    {
        public UnknownIdentifier(string value) : base(value, IdentifierType.Unknown)
        {

        }
    }

}
