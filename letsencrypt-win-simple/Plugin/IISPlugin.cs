using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Text.RegularExpressions;

namespace LetsEncrypt.ACME.Simple
{
    public class IISPlugin : Plugin
    {
        public override string Name => "IIS";

        static Version iisVersion;

        public override List<Target> GetTargets()
        {
            Console.WriteLine("\nScanning IIS Site Bindings for Hosts");
            Log.Information("Scanning IIS Site Bindings for Hosts");

            var result = new List<Target>();

            iisVersion = GetIisVersion();
            if (iisVersion.Major == 0)
            {
                Console.WriteLine(" IIS Version not found in windows registry. Skipping scan.");
                Log.Information("IIS Version not found in windows registry. Skipping scan.");
            }
            else
            {
                using (var iisManager = new ServerManager())
                {
                    foreach (var site in iisManager.Sites)
                    {
                        List<Target> returnHTTP = new List<Target>();
                        List<Target> siteHTTPS = new List<Target>();
                        List<Target> siteHTTP = new List<Target>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" && !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                returnHTTP.Add(new Target() { SiteId = site.Id, Host = binding.Host, WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath, PluginName = Name });
                            }
                            //Get HTTPS sites that aren't IDN
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "https" && !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                siteHTTPS.Add(new Target() { SiteId = site.Id, Host = binding.Host, WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath, PluginName = Name });
                            }
                        }

                        siteHTTP.AddRange(returnHTTP);
                        if (Program.Options.HideHTTPS == true)
                        {
                            foreach (var bindingHTTPS in siteHTTPS)
                            {
                                foreach (var bindingHTTP in siteHTTP)
                                {
                                    if (bindingHTTPS.Host == bindingHTTP.Host)
                                    {
                                        //If there is already an HTTPS binding for the same host, don't show the HTTP binding
                                        returnHTTP.Remove(bindingHTTP);
                                    }
                                }
                            }
                            result.AddRange(returnHTTP);
                        }
                        else
                        {
                            result.AddRange(returnHTTP);
                        }
                    }
                }

                if (result.Count == 0)
                {
                    Console.WriteLine(" No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                    Log.Information("No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                }
            }

            return result;
        }
        public override List<Target> GetSites()
        {

            Console.WriteLine("\nScanning IIS Sites");
            Log.Information("Scanning IIS Sites");

            var result = new List<Target>();

            iisVersion = GetIisVersion();
            if (iisVersion.Major == 0)
            {
                Console.WriteLine(" IIS Version not found in windows registry. Skipping scan.");
                Log.Information("IIS Version not found in windows registry. Skipping scan.");
            }
            else
            {
                using (var iisManager = new ServerManager())
                {
                    foreach (var site in iisManager.Sites)
                    {
                        List<Target> returnHTTP = new List<Target>();
                        List<string> Hosts = new List<string>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" && !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                Hosts.Add(binding.Host);

                                returnHTTP.Add(new Target() { SiteId = site.Id, Host = binding.Host, WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath, PluginName = Name });
                            }
                        }
                        if (Hosts.Count <= 100)
                        {
                            result.Add(new Target() { SiteId = site.Id, Host = site.Name, WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath, PluginName = Name, AlternativeNames = Hosts });
                        }
                        else
                        {
                            Console.WriteLine($" {site.Name} has too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                            Log.Error("{Name} has too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.", site.Name);
                        }
                    }
                }

                if (result.Count == 0)
                {
                    Console.WriteLine(" No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                    Log.Information("No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                }
            }

            return result;
        }

        string sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public override void BeforeAuthorize(Target target, string answerPath)
        {
            var directory = Path.GetDirectoryName(answerPath);
            var webConfigPath = Path.Combine(directory, "web.config");
            
            Console.WriteLine($" Writing web.config to add extensionless mime type to {webConfigPath}");
            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);
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
            Log.Error("Authorize failed: This could be caused by IIS not being setup to handle extensionless static files.Here's how to fix that: 1.In IIS manager goto Site/ Server->Handler Mappings->View Ordered List 2.Move the StaticFile mapping above the ExtensionlessUrlHandler mappings. (like this http://i.stack.imgur.com/nkvrL.png) 3.If you need to make changes to your web.config file, update the one at {sourceFilePath}", sourceFilePath);
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            using (var iisManager = new ServerManager())
            {
                var site = GetSite(target, iisManager);
                List<string> hosts = new List<string>();
                if (!Program.Options.SAN)
                {
                    hosts.Add(target.Host);
                }
                if (target.AlternativeNames != null)
                {
                    if (target.AlternativeNames.Count > 0)
                    {
                        hosts.AddRange(target.AlternativeNames);
                    }
                }

                foreach (var host in hosts)
                {
                    var existingBinding = (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b).FirstOrDefault();
                    if (existingBinding != null)
                    {
                        Console.WriteLine($" Updating Existing https Binding");
                        Log.Information("Updating Existing https Binding");
                        existingBinding.CertificateStoreName = store.Name;
                        existingBinding.CertificateHash = certificate.GetCertHash();
                    }
                    else
                    {
                        Console.WriteLine($" Adding https Binding");
                        Log.Information("Adding https Binding");
                        var existingHTTPBinding = (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b).FirstOrDefault();
                        string HTTPEndpoint = existingHTTPBinding.EndPoint.ToString();
                        string IP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'), (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

                        if (IP == "0.0.0.0")
                        {
                            IP = ""; //Remove the IP if it is 0.0.0.0 That happens if an IP wasn't set on the HTTP site and it used any available IP
                        }

                        var iisBinding = site.Bindings.Add(IP + ":443:" + host, certificate.GetCertHash(), store.Name);
                        iisBinding.Protocol = "https";

                        if (iisVersion.Major >= 8)
                            iisBinding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                    }
                }
                Console.WriteLine($" Committing binding changes to IIS");
                Log.Information("Committing binding changes to IIS");
                iisManager.CommitChanges();
            }
        }

        //This doesn't take any certificate info to enable centralized ssl
        public override void Install(Target target)
        {
            try
            {
                using (var iisManager = new ServerManager())
                {
                    var site = GetSite(target, iisManager);

                    List<string> hosts = new List<string>();
                    if (!Program.Options.SAN)
                    {
                        hosts.Add(target.Host);
                    }
                    hosts.AddRange(target.AlternativeNames);

                    foreach (var host in hosts)
                    {
                        var existingBinding = (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b).FirstOrDefault();
                        if (existingBinding != null)
                        {
                            Console.WriteLine($" Updating Existing https Binding");
                            Log.Information("Updating Existing https Binding");
                            if (iisVersion.Major >= 8 && existingBinding.GetAttributeValue("sslFlags").ToString() != "2")
                            {
                                //IIS 8+ and not using centralized SSL
                                existingBinding.CertificateStoreName = null;
                                existingBinding.CertificateHash = null;
                                existingBinding.SetAttributeValue("sslFlags", 2);
                            }
                            else if (!(iisVersion.Major >= 8))
                            {
                                Log.Error("You aren't using IIS 8 or greater, so centralized SSL is not supported");
                                //Not using IIS 8+ so can't set centralized certificates
                                throw new InvalidOperationException("You aren't using IIS 8 or greater, so centralized SSL is not supported");
                            }
                        }
                        else
                        {
                            Console.WriteLine($" Adding Central SSL https Binding");
                            Log.Information("Adding Central SSL https Binding");
                            var existingHTTPBinding = (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b).FirstOrDefault();
                            string HTTPEndpoint = existingHTTPBinding.EndPoint.ToString();
                            string IP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'), (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

                            if (IP == "0.0.0.0")
                            {
                                IP = ""; //Remove the IP if it is 0.0.0.0 That happens if an IP wasn't set on the HTTP site and it used any available IP
                            }

                            var iisBinding = site.Bindings.Add(IP + ":443:" + host, "https");

                            if (iisVersion.Major >= 8)
                                iisBinding.SetAttributeValue("sslFlags", 2); // Enable Centralized Certificate Store
                        }
                    }
                    Console.WriteLine($" Committing binding changes to IIS");
                    Log.Information("Committing binding changes to IIS");
                    iisManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Setting Binding: " + ex.Message.ToString());
                Log.Error("Error Setting Binding {@ex}", ex);
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
            Log.Error("Unable to find IIS site ID # {SiteId} for binding {this}", target.SiteId, this);
            throw new System.Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }
    }
}
