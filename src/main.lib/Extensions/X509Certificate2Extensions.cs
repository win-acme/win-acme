using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Extensions
{
    public static partial class X509Certificate2Extensions
    {
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
