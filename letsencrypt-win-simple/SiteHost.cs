using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple
{
    public class TargetBinding
    {
        public string Host { get; set; }
        public string PhysicalPath { get; set; }
        public long SiteId { get; set; }

        public Site GetSite(ServerManager iisManager)
        {
            return iisManager.Sites[(int)SiteId];
        }

        public string GetPhysicalPath(ServerManager iisManager)
        {
            var site = GetSite(iisManager);
            return site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
        }

        public override string ToString() => $"{Host} ({PhysicalPath})";
    }
}