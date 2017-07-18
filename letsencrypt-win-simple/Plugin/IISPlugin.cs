using Microsoft.Web.Administration;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace LetsEncrypt.ACME.Simple
{
    public class IISPlugin : Plugin
    {
        public override string Name => "IIS";

        private static Version _iisVersion;

        public override List<Target> GetTargets()
        {
            Log.Information("Scanning IIS Site Bindings for Hosts");

            var result = new List<Target>();

            _iisVersion = GetIisVersion();
            if (_iisVersion.Major == 0)
            {
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
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (returnHTTP.Where(h => h.Host == binding.Host).Count() == 0)
                                {
                                    returnHTTP.Add(new Target()
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                        PluginName = Name
                                    });
                                }
                            }
                            //Get HTTPS sites that aren't IDN
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "https" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (siteHTTPS.Where(h => h.Host == binding.Host).Count() == 0)
                                {
                                    siteHTTPS.Add(new Target()
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                        PluginName = Name
                                    });
                                }
                            }
                        }

                        siteHTTP.AddRange(returnHTTP);
                        if (Program.Options.HideHttps == true)
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
                    Log.Information(
                        "No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                }
            }

            return result;
        }

        public override List<Target> GetSites()
        {
            Log.Information("Scanning IIS Sites");

            var result = new List<Target>();

            _iisVersion = GetIisVersion();
            if (_iisVersion.Major == 0)
            {
                Log.Information("IIS Version not found in windows registry. Skipping scan.");
            }
            else
            {
                using (var iisManager = new ServerManager())
                {
                    foreach (var site in iisManager.Sites)
                    {
                        List<Target> returnHTTP = new List<Target>();
                        List<string> hosts = new List<string>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (hosts.Where(h => h == binding.Host).Count() == 0)
                                {
                                    hosts.Add(binding.Host);
                                }

                                returnHTTP.Add(new Target()
                                {
                                    SiteId = site.Id,
                                    Host = binding.Host,
                                    WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                    PluginName = Name
                                });
                            }
                        }
                        if (hosts.Count <= 100 && hosts.Count > 0)
                        {
                            result.Add(new Target()
                            {
                                SiteId = site.Id,
                                Host = site.Name,
                                WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                PluginName = Name,
                                AlternativeNames = hosts
                            });
                        }
                        else if (hosts.Count > 0)
                        {
                            Log.Error(
                                "{Name} has too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.",
                                site.Name);
                        }
                    }
                }

                if (result.Count == 0)
                {
                    Log.Information(
                        "No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                }
            }

            return result.OrderBy(r => r.SiteId).ToList();
        }

        private readonly string _sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            var directory = Path.GetDirectoryName(answerPath);
            var webConfigPath = Path.Combine(directory, "web.config");
            
            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);
            File.Copy(_sourceFilePath, webConfigPath, true);
        }

        public override void OnAuthorizeFail(Target target)
        {
            Log.Error(
                "Authorize failed: This could be caused by IIS not being setup to handle extensionless static files.Here's how to fix that: \n1.In IIS manager goto Site/ Server->Handler Mappings->View Ordered List \n2.Move the StaticFile mapping above the ExtensionlessUrlHandler mappings. (like this http://i.stack.imgur.com/nkvrL.png) \n3.If you need to make changes to your web.config file, update the one at {_sourceFilePath}",
                _sourceFilePath);
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            using (var iisManager = new ServerManager())
            {
                var site = GetSite(target, iisManager);
                List<string> hosts = new List<string>();
                if (!Program.Options.San)
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
                    var existingBinding =
                        (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b).FirstOrDefault();
                    if (existingBinding != null)
                    {
                        Log.Information("Updating Existing https Binding");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Log.Information("IIS will serve the new certificate after the Application Pool Idle Timeout time has been reached.");
                        Console.ResetColor();

                        existingBinding.CertificateStoreName = store.Name;
                        existingBinding.CertificateHash = certificate.GetCertHash();
                    }
                    else
                    {
                        Log.Information("Adding https Binding");
                        var existingHTTPBinding =
                            (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b)
                                .FirstOrDefault();
                        if (existingHTTPBinding != null)
                        //This had been a fix for the multiple site San cert, now it's just a precaution against erroring out
                        {
                            string IP = GetIP(existingHTTPBinding.EndPoint.ToString(), host);

                            var iisBinding = site.Bindings.Add(IP + ":443:" + host, certificate.GetCertHash(),
                                store.Name);
                            iisBinding.Protocol = "https";

                            if (_iisVersion.Major >= 8)
                                iisBinding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                        }
                        else
                        {
                            Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                        }
                    }
                }
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
                    if (!Program.Options.San)
                    {
                        hosts.Add(target.Host);
                    }
                    if (target.AlternativeNames != null && target.AlternativeNames.Any())
                    {
                        hosts.AddRange(target.AlternativeNames);
                    }

                    foreach (var host in hosts)
                    {
                        var existingBinding =
                            (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b)
                                .FirstOrDefault();
                        if (!(_iisVersion.Major >= 8))
                        {
                            var errorMessage = "You aren't using IIS 8 or greater, so centralized SSL is not supported";
                            Log.Error(errorMessage);
                            //Not using IIS 8+ so can't set centralized certificates
                            throw new InvalidOperationException(errorMessage);
                        }
                        else if (existingBinding != null)
                        {
                            if (existingBinding.GetAttributeValue("sslFlags").ToString() != "3")
                            {
                                Log.Information("Updating Existing https Binding");
                                //IIS 8+ and not using centralized SSL with SNI
                                existingBinding.CertificateStoreName = null;
                                existingBinding.CertificateHash = null;
                                existingBinding.SetAttributeValue("sslFlags", 3);
                            }
                            else
                            {
                                Log.Information(
                                    "You specified Central SSL, have an existing binding using Central SSL with SNI, so there is nothing to update for this binding");
                            }
                        }
                        else
                        {
                            Log.Information("Adding Central SSL https Binding");
                            var existingHTTPBinding =
                                (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b)
                                    .FirstOrDefault();
                            if (existingHTTPBinding != null)
                            //This had been a fix for the multiple site San cert, now it's a precaution against erroring out
                            {
                                string IP = GetIP(existingHTTPBinding.EndPoint.ToString(), host);

                                var iisBinding = site.Bindings.Add(IP + ":443:" + host, "https");

                                iisBinding.SetAttributeValue("sslFlags", 3);
                                // Enable Centralized Certificate Store with SNI
                            }
                            else
                            {
                                Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                            }
                        }
                    }
                    Log.Information("Committing binding changes to IIS");
                    iisManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error Setting Binding {@ex}", ex);
                throw new InvalidProgramException(ex.Message);
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

        public override void Renew(Target target)
        {
            _iisVersion = GetIisVersion();
            Auto(target);
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information("Deleting answer");
            File.Delete(answerPath);

            try
            {
                if (Properties.Settings.Default.CleanupFolders == true)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
                    var files = Directory.GetFiles(folderPath);

                    if (files.Length == 1)
                    {
                        if (files[0] == (folderPath + "web.config"))
                        {
                            Log.Information("Deleting web.config");
                            File.Delete(files[0]);
                            Log.Information("Deleting {folderPath}", folderPath);
                            Directory.Delete(folderPath);

                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Log.Information("Deleting {filePathFirstDirectory}", filePathFirstDirectory);
                            Directory.Delete(filePathFirstDirectory);
                        }
                        else
                        {
                            Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                        }
                    }
                    else
                    {
                        Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }

        private Site GetSite(Target target, ServerManager iisManager)
        {
            foreach (var site in iisManager.Sites)
            {
                if (site.Id == target.SiteId)
                    return site;
            }
            throw new System.Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }

        private string GetIP(string HTTPEndpoint, string host)
        {
            string IP = "*";
            string HTTPIP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'),
                (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

            if (_iisVersion.Major >= 8 && HTTPIP != "0.0.0.0")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\r\nWarning creating HTTPS Binding for {host}.");
                Console.ResetColor();
                Console.WriteLine(
                    "The HTTP binding is IP specific; the app can create it. However, if you have other HTTPS sites they will all get an invalid certificate error until you manually edit one of their HTTPS bindings.");
                Console.WriteLine("\r\nYou need to edit the binding, turn off SNI, click OK, edit it again, enable SNI and click OK. That should fix the error.");
                Console.WriteLine("\r\nOtherwise, manually create the HTTPS binding and rerun the application.");
                Console.WriteLine("\r\nYou can see https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/HTTPS-Binding-With-Specific-IP for more information.");
                Console.WriteLine(
                    "\r\nPress Y to acknowledge this and continue. Press any other key to stop installing the certificate");
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                {
                    IP = HTTPIP;
                }
                else
                {
                    throw new Exception(
                        "HTTPS Binding not created due to HTTP binding having specific IP; Manually create the HTTPS binding and retry");
                }
            }
            else if (HTTPIP != "0.0.0.0")
            {
                IP = HTTPIP;
            }
            return IP;
        }

        public override void CheckIfDownloadable(Uri uri)
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadData(uri);
            }
            base.CheckIfDownloadable(uri);
        }
    }
}