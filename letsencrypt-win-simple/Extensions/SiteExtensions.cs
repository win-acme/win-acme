using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple.Extensions
{
    public static class SiteExtensions
    {
        public static string WebRoot(this Site site)
        {
            return site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
        }
    }
}
