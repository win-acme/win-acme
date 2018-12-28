using Microsoft.Web.Administration;
using System.Linq;
using static PKISharp.WACS.Clients.IISClient;

namespace PKISharp.WACS.Extensions
{
    public static class BindingExtensions
    {
        public static SSLFlags SSLFlags(this Binding binding)
        {
            return (SSLFlags)binding.Attributes.
                    Where(x => x.Name == "sslFlags").
                    Where(x => x.Value != null).
                    Select(x => int.Parse(x.Value.ToString())).
                    FirstOrDefault();
        }

        public static bool HasSSLFlags(this Binding binding, SSLFlags flags)
        {
            return (binding.SSLFlags() & flags) == flags;
        }
    }
}
