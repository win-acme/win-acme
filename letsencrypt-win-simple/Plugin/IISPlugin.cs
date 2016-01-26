using Microsoft.Web.Administration;
using Microsoft.Win32;
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

        static Version iisVersion;

        public override List<Target> GetTargets()
        {
            Console.WriteLine("\nScanning IIS 7 Site Bindings for Hosts");

            var result = new List<Target>();

            iisVersion = GetIisVersion();
            if (iisVersion.Major == 0)
            {
                Console.WriteLine(" IIS Version not found in windows registry. Skipping scan.");
            }
            else
            {
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
            }

            return result;
        }

        //string webConfig = Properties.Settings.Default.IISWebConfig;
        string sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");


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
            //File.WriteAllText(webConfigPath, webConfig);
            File.Copy(sourceFilePath, webConfigPath, true);
        }

        public override void OnAuthorizeFail(Target target)
        {
            Console.WriteLine(@"

This could be caused by IIS not being setup to handle extensionless static
files. Here's how to fix that:
1. In IIS manager goto Site/Server->Handler Mappings->View Ordered List
2. Move the StaticFile mapping above the ExtensionlessUrlHandler mappings.
(like this http://i.stack.imgur.com/nkvrL.png)
3. If you need to make changes to your web.config file, update the one
at " + sourceFilePath);
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
                    existingBinding.CertificateStoreName = store.Name;
                    existingBinding.CertificateHash = certificate.GetCertHash();
                }
                else
                {
                    Console.WriteLine($" Adding https Binding");
                    var existingHTTPBinding = (from b in site.Bindings where b.Host == target.Host && b.Protocol == "http" select b).FirstOrDefault();
                    string HTTPEndpoint = existingHTTPBinding.EndPoint.ToString();
                    string IP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'), (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

                    if (IP == "0.0.0.0")
                    {
                        IP = ""; //Remove the IP if it is 0.0.0.0 That happens if an IP wasn't set on the HTTP site and it used any available IP
                    }

                    var iisBinding = site.Bindings.Add(IP + ":443:" + target.Host, certificate.GetCertHash(), store.Name);
                    iisBinding.Protocol = "https";

                    if (iisVersion.Major >= 8)
                        iisBinding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                }

                Console.WriteLine($" Committing binding changes to IIS");
                iisManager.CommitChanges();
            }
        }

        //This doesn't take any certificate info to enable centralized ssl
        public void Install(Target target)
        {
            try
            {
                using (var iisManager = new ServerManager())
                {
                    var site = GetSite(target, iisManager);

                    var existingBinding = (from b in site.Bindings where b.Host == target.Host && b.Protocol == "https" select b).FirstOrDefault();
                    if (existingBinding != null)
                    {
                        Console.WriteLine($" Updating Existing https Binding");
                        if (iisVersion.Major >= 8 && existingBinding.GetAttributeValue("sslFlags").ToString() != "2")
                        {
                            //IIS 8+ and not using centralized SSL
                            existingBinding.CertificateStoreName = null;
                            existingBinding.CertificateHash = null;
                            existingBinding.SetAttributeValue("sslFlags", 2);
                        }
                        else if (!(iisVersion.Major >= 8))
                        {
                            //Not using IIS 8+ so can't set centralized certificates
                            throw new InvalidOperationException("You aren't using IIS 8 or greater, so centralized SSL is not supported");
                        }
                    }
                    else
                    {
                        Console.WriteLine($" Adding Central SSL https Binding");
                        var existingHTTPBinding = (from b in site.Bindings where b.Host == target.Host && b.Protocol == "http" select b).FirstOrDefault();
                        string HTTPEndpoint = existingHTTPBinding.EndPoint.ToString();
                        string IP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'), (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

                        if (IP == "0.0.0.0")
                        {
                            IP = ""; //Remove the IP if it is 0.0.0.0 That happens if an IP wasn't set on the HTTP site and it used any available IP
                        }

                        var iisBinding = site.Bindings.Add(IP + ":443:" + target.Host, "https");

                        if (iisVersion.Major >= 8)
                            iisBinding.SetAttributeValue("sslFlags", 2); // Enable Centralized Certificate Store
                    }

                    Console.WriteLine($" Committing binding changes to IIS");
                    iisManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Setting Binding: " + ex.Message.ToString());
                throw new InvalidProgramException(ex.Message.ToString());
            }
        }

        public Version GetIisVersion()
        {
            using (RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (componentsKey != null)
                {
                    int majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    int minorVersion = (int)componentsKey.GetValue("MinorVersion", -1);

                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }

                return new Version(0, 0);
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
