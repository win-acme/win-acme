using Org.BouncyCastle.Asn1.X509;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class X509CertificateExtensions
    {
        
        public static string? CommonName(this X509Name target, [NotNullWhen(true)] bool allowFull = false) => 
            target.GetValueList(X509Name.CN).FirstOrDefault() ?? (allowFull ? target.ToString() : null);
    }
}