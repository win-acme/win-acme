using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public class IISPlugin : Plugin
    {
        public override string Name => "IIS";

        public override List<Target> GetTargets()
        {
            Console.WriteLine("\nScanning IIS 7 Site Bindings for Hosts");

            var result = new List<Target>();
            using (var iisManager = new ServerManager())
            {
                foreach (var site in iisManager.Sites)
                {
                    foreach (var binding in site.Bindings)
                    {
                        if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http")
                            result.Add(new Target() { SiteId = site.Id, Host = binding.Host, WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath, PluginName = Name });
                    }
                }
            }

            if (result.Count == 0)
            {
                Console.WriteLine(" No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
            }

            return result;
        }

        const string webConfig = @"<?xml version = ""1.0"" encoding=""UTF-8""?>
 <configuration>
     <system.webServer>
         <staticContent>
             <mimeMap fileExtension = "".*"" mimeType=""text/json"" />
         </staticContent>
     </system.webServer>
 </configuration>";

        // all this would do is move the handler to the bottom, which is the last place you want it.
        //<handlers>
        //    <remove name = "StaticFile" />
        //    < add name="StaticFile" path="*." verb="*" type="" modules="StaticFileModule,DefaultDocumentModule,DirectoryListingModule" scriptProcessor="" resourceType="Either" requireAccess="Read" allowPathInfo="false" preCondition="" responseBufferLimit="4194304" />
        //</handlers>

        // this can work sometimes
        //<handlers>
        //    <clear />
        //    <add name = ""StaticFile"" path=""*."" verb=""*"" type="""" modules=""StaticFileModule,DefaultDocumentModule,DirectoryListingModule"" scriptProcessor="""" resourceType=""Either"" requireAccess=""Read"" allowPathInfo=""false"" preCondition="""" responseBufferLimit=""4194304"" />
        //</handlers>

        public override void BeforeAuthorize(Target target, string answerPath)
        {
            var directory = Path.GetDirectoryName(answerPath);
            var webConfigPath = Path.Combine(directory, "web.config");
            Console.WriteLine($" Writing web.config to add extensionless mime type to {webConfigPath}");
            File.WriteAllText(webConfigPath, webConfig);
        }

        public override void OnAuthorizeFail(Target target)
        {
            Console.WriteLine(@"

This could be caused by IIS not being setup to handle extensionless static
files. Here's how to fix that:
1. In IIS manager goto Site/Server->Handler Mappings->View Ordered List
2. Move the StaticFile mapping above the ExtensionlessUrlHandler mappings.
(like this http://i.stack.imgur.com/nkvrL.png)");
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            using (var iisManager = new ServerManager())
            {
                var site = GetSite(target, iisManager);
                var existingBinding = (from b in site.Bindings where b.Host == target.Host && b.Protocol == "https" select b).FirstOrDefault();
                if (existingBinding != null)
                {
                    Console.WriteLine($" Updating Existing https Binding");
                    existingBinding.CertificateHash = certificate.GetCertHash();
                    existingBinding.CertificateStoreName = store.Name;
                }
                else
                {
                    Console.WriteLine($" Adding https Binding");
                    var iisBinding = site.Bindings.Add(":443:" + target.Host, certificate.GetCertHash(), store.Name);
                    iisBinding.Protocol = "https";
                }

                Console.WriteLine($" Commiting binding changes to IIS");
                iisManager.CommitChanges();
            }
        }

        Site GetSite(Target target, ServerManager iisManager)
        {
            foreach (var site in iisManager.Sites)
            {
                if (site.Id == target.SiteId)
                    return site;
            }
            throw new System.Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }
    }
}
