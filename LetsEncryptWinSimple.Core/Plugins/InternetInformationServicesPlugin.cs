using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using LetsEncryptWinSimple.Core.Configuration;
using LetsEncryptWinSimple.Core.Interfaces;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using Serilog;

namespace LetsEncryptWinSimple.Core.Plugins
{
    public class InternetInformationServicesPlugin : IPlugin
    {
        protected IOptions Options;
        protected IConsoleService ConsoleService;
        protected IPluginService PluginService;
        public InternetInformationServicesPlugin(IOptions options, IConsoleService consoleService,
            IPluginService pluginService)
        {
            Options = options;
            ConsoleService = consoleService;
            PluginService = pluginService;
        }

        public string Name => "IIS";

        private static Version _iisVersion;

        public List<Target> GetTargets()
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
                        var returnHttp = new List<Target>();
                        var siteHttps = new List<Target>();
                        var siteHttp = new List<Target>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!string.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (returnHttp.All(h => h.Host != binding.Host))
                                {
                                    returnHttp.Add(new Target
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                        PluginName = Name
                                    });
                                }
                            }
                            //Get HTTPS sites that aren't IDN
                            if (!string.IsNullOrEmpty(binding.Host) && binding.Protocol == "https" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (siteHttps.All(h => h.Host != binding.Host))
                                {
                                    siteHttps.Add(new Target
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                        PluginName = Name
                                    });
                                }
                            }
                        }

                        siteHttp.AddRange(returnHttp);
                        if (Options.HideHttps)
                        {
                            foreach (var bindingHttps in siteHttps)
                            {
                                foreach (var bindingHttp in siteHttp)
                                {
                                    if (bindingHttps.Host == bindingHttp.Host)
                                    {
                                        //If there is already an HTTPS binding for the same host, don't show the HTTP binding
                                        returnHttp.Remove(bindingHttp);
                                    }
                                }
                            }
                            result.AddRange(returnHttp);
                        }
                        else
                        {
                            result.AddRange(returnHttp);
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

        public List<Target> GetSites()
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
                        var hosts = new List<string>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!string.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (hosts.All(h => h != binding.Host))
                                {
                                    hosts.Add(binding.Host);
                                }
                            }
                        }
                        if (hosts.Count <= 100 && hosts.Count > 0)
                        {
                            result.Add(new Target
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

        public void BeforeAuthorize(Target target, string answerPath, string token)
        {
            var directory = Path.GetDirectoryName(answerPath);
            if (directory == null)
                throw new NullReferenceException("directory");

            var webConfigPath = Path.Combine(directory, "web.config");

            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);
            File.Copy(_sourceFilePath, webConfigPath, true);
        }

        public void OnAuthorizeFail(Target target)
        {
            Log.Error(
                "Authorize failed: This could be caused by IIS not being setup to handle extensionless static files.Here's how to fix that: \n1.In IIS manager goto Site/ Server->Handler Mappings->View Ordered List \n2.Move the StaticFile mapping above the ExtensionlessUrlHandler mappings. (like this http://i.stack.imgur.com/nkvrL.png) \n3.If you need to make changes to your web.config file, update the one at {_sourceFilePath}",
                _sourceFilePath);
        }

        public void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            using (var iisManager = new ServerManager())
            {
                var site = GetSite(target, iisManager);
                var hosts = new List<string>();
                if (!Options.San)
                {
                    hosts.Add(target.Host);
                }
                if (target.AlternativeNames != null && target.AlternativeNames.Count > 0)
                {
                    hosts.AddRange(target.AlternativeNames);
                }

                foreach (var host in hosts)
                {
                    var existingBinding =
                        (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b).FirstOrDefault();
                    if (existingBinding != null)
                    {
                        Log.Information("Updating Existing https Binding");
                        Log.Warning("IIS will serve the new certificate after the Application Pool Idle Timeout time has been reached.");

                        existingBinding.CertificateStoreName = store.Name;
                        existingBinding.CertificateHash = certificate.GetCertHash();
                    }
                    else
                    {
                        Log.Information("Adding https Binding");
                        var existingHttpBinding = site.Bindings.FirstOrDefault(b => b.Host == host && b.Protocol == "http");
                        if (existingHttpBinding != null)
                        //This had been a fix for the multiple site San cert, now it's just a precaution against erroring out
                        {
                            var ipAddress = GetIpAddress(existingHttpBinding.EndPoint.ToString(), host);
                            var iisBinding = site.Bindings.Add($"{ipAddress}:443:{host}", certificate.GetCertHash(), store.Name);
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
        public void Install(Target target)
        {
            try
            {
                using (var iisManager = new ServerManager())
                {
                    var site = GetSite(target, iisManager);

                    var hosts = new List<string>();
                    if (!Options.San)
                    {
                        hosts.Add(target.Host);
                    }
                    if (target.AlternativeNames != null && target.AlternativeNames.Any())
                    {
                        hosts.AddRange(target.AlternativeNames);
                    }

                    foreach (var host in hosts)
                    {
                        var existingBinding = site.Bindings.FirstOrDefault(b => b.Host == host && b.Protocol == "https");
                        if (!(_iisVersion.Major >= 8))
                        {
                            var errorMessage =$"Detected IIS version {_iisVersion.Major}. You aren't using IIS 8 or greater, so centralized SSL is not supported";
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
                            var existingHttpBinding = site.Bindings.FirstOrDefault(b => b.Host == host && b.Protocol == "http");
                            if (existingHttpBinding != null)
                            //This had been a fix for the multiple site San cert, now it's a precaution against erroring out
                            {
                                var ipAddress = GetIpAddress(existingHttpBinding.EndPoint.ToString(), host);

                                var iisBinding = site.Bindings.Add($"{ipAddress}:443:{host}", "https");

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
            using (var componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (componentsKey != null)
                {
                    var majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    var minorVersion = (int)componentsKey.GetValue("MinorVersion", -1);

                    if (majorVersion != -1 && minorVersion != -1)
                        return new Version(majorVersion, minorVersion);
                }

                return new Version(0, 0);
            }
        }

        public void Renew(Target target)
        {
            _iisVersion = GetIisVersion();
            PluginService.DefaultAction(target);
        }

        public void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information("Deleting answer");
            File.Delete(answerPath);

            try
            {
                if (!Properties.Settings.Default.CleanupFolders)
                    return;

                var folderPath = answerPath.Remove(answerPath.Length - token.Length, token.Length);
                var files = Directory.GetFiles(folderPath);

                if (files.Length == 1)
                {
                    if (files[0] == $"{folderPath}web.config")
                    {
                        Log.Information("Deleting web.config");
                        File.Delete(files[0]);
                        Log.Information("Deleting {folderPath}", folderPath);
                        Directory.Delete(folderPath);

                        var filePathFirstDirectory = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, 
                            filePath.Remove(filePath.IndexOf("/", StringComparison.Ordinal), 
                            filePath.Length - filePath.IndexOf("/", StringComparison.Ordinal))));
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
            catch (Exception ex)
            {
                Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        public void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            if(directory == null)
                throw new NullReferenceException("directory");
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
            throw new Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }

        private string GetIpAddress(string httpEndpoint, string host)
        {
            var ipAddress = "*";
            var httpIpAddress = httpEndpoint.Remove(httpEndpoint.IndexOf(':'),
                httpEndpoint.Length - httpEndpoint.IndexOf(':'));

            if (_iisVersion.Major >= 8 && httpIpAddress != "0.0.0.0")
            {
                Log.Warning($"\r\nWarning creating HTTPS Binding for {host}.");

                ConsoleService.WriteLine(
                    "The HTTP binding is IP specific; the app can create it. However, if you have other HTTPS sites they will all get an invalid certificate error until you manually edit one of their HTTPS bindings.");
                ConsoleService.WriteLine("\r\nYou need to edit the binding, turn off SNI, click OK, edit it again, enable SNI and click OK. That should fix the error.");
                ConsoleService.WriteLine("\r\nOtherwise, manually create the HTTPS binding and rerun the application.");
                ConsoleService.WriteLine("\r\nYou can see https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/HTTPS-Binding-With-Specific-IP for more information.");
                var confirmed = ConsoleService.PromptYesNo("\r\nDo you want to continue?.");
                if (confirmed)
                {
                    ipAddress = httpIpAddress;
                }
                else
                {
                    throw new Exception(
                        "HTTPS Binding not created due to HTTP binding having specific IP; Manually create the HTTPS binding and retry");
                }
            }
            else if (httpIpAddress != "0.0.0.0")
            {
                ipAddress = httpIpAddress;
            }
            return ipAddress;
        }

        public void PrintMenu()
        {
        }

        public void Auto(Target target)
        {
        }

        public void HandleMenuResponse(string response, List<Target> targets)
        {
        }
    }
}