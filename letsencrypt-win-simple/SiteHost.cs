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
            foreach (var site in iisManager.Sites)
            {
                if (site.Id == SiteId)
                    return site;
            }
            throw new System.Exception($"Unable to find IIS site ID #{SiteId} for binding {this}");
        }

        public string GetPhysicalPath(ServerManager iisManager)
        {
            var site = GetSite(iisManager);
            return site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
        }

        public override string ToString() => $"{Host} ({PhysicalPath})";
    }
}