using PKISharp.WACS.Extensions;
using System;
using System.Diagnostics;
using System.Globalization;

namespace PKISharp.WACS.DomainObjects
{
    public enum IdentifierType
    {
        Unknown,
        IpAddress,
        DnsName,
        UpnName
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
        public int CompareTo(object? obj) => (obj as Identifier)?.ToString()?.CompareTo(ToString()) ?? -1;
        public int CompareTo(Identifier? other) => other?.ToString()?.CompareTo(ToString()) ?? -1;
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
        public IpIdentifier(string value) : base(value, IdentifierType.IpAddress)
        {

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
