using System.Diagnostics;

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
    public class Identifier
    {
        public Identifier(string value, IdentifierType identifierType = IdentifierType.Unknown)
        {
            Value = value;
            Type = identifierType;
        }
        public IdentifierType Type { get; set; }
        public string Value { get; set; }
    }
}
