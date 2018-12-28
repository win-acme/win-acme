using Microsoft.Web.Administration;

namespace PKISharp.WACS.Extensions
{
    public static class SiteExtensions
    {
        public static string WebRoot(this Site site)
        {
            return site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
        }
    }
}
