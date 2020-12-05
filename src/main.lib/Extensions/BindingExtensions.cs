using Microsoft.Web.Administration;
using PKISharp.WACS.Clients.IIS;
using System.Linq;

namespace PKISharp.WACS.Extensions
{
    public static class BindingExtensions
    {
        public static SSLFlags SSLFlags(this Binding binding)
        {
            // IIS 7.x is very picky about accessing the sslFlags attribute,
            // if we don't do it this way, it will crash
            return (SSLFlags)binding.Attributes.
                    Where(x => x.Name == "sslFlags").
                    Where(x => x.Value != null).
                    Select(x => int.Parse(x.Value.ToString()!)).
                    FirstOrDefault();
        }

        public static bool HasSSLFlags(this Binding binding, SSLFlags flags) => (binding.SSLFlags() & flags) == flags;

        /// <summary>
        /// For for #1083
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public static string NormalizedBindingInformation(this Binding binding)
        {
            if (binding.BindingInformation.StartsWith(":"))
            {
                return $"*{binding.BindingInformation}";
            }
            else
            {
                return binding.BindingInformation;
            }
        }
    }
}
