using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple
{
    public class IISPlugin : Plugin
    {
        private Version _iisVersion = GetIisVersion();
        private IdnMapping _idnMapping = new IdnMapping();

        public override string Name => "IIS";

        public override List<Target> GetTargets()
        {
            Program.Log.Debug("Scanning IIS site bindings for hosts");
            if (_iisVersion.Major == 0)
            {
                Program.Log.Warning("IIS version not found in windows registry. Skipping scan.");
            }
            else
            {
                using (var iisManager = new ServerManager())
                {
                    // Get all bindings matched together with their respective sites
                    var siteBindings = iisManager.Sites.
                        Where(s => s.State == ObjectState.Started).
                        SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                        Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host));

                    // Option: hide http bindings when there are already https equivalents
                    if (Program.Options.HideHttps)
                    {
                        siteBindings = siteBindings.Where(sb => 
                            sb.binding.Protocol == "http" &&
                            !sb.site.Bindings.Any(other => other.Protocol == "https" && sb.binding.Host == other.Host));
                    }

                    var targets = siteBindings.
                        Select(sb => new { idn = _idnMapping.GetAscii(sb.binding.Host), sb.site, sb.binding }).
                        Select(sbi => new Target
                        {
                            SiteId = sbi.site.Id,
                            Host = sbi.idn,
                            WebRootPath = sbi.site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                            PluginName = Name
                        }).
                        DistinctBy(t => t.Host).
                        OrderBy(t => t.SiteId).
                        ToList();
     
                    if (targets.Count() == 0)
                    {
                        Program.Log.Warning("No IIS bindings with host names were found. A host name is required to verify domain ownership.");
                    }
                    return targets;
                }
            }
            return new List<Target>();
        }

        public override List<Target> GetSites()
        {
            var result = new List<Target>();
            Program.Log.Debug("Scanning IIS sites");
            if (_iisVersion.Major == 0)
            {
                Program.Log.Warning("IIS version not found in windows registry. Skipping scan.");
            }
            else
            {
                using (var iisManager = new ServerManager())
                {
                    // Get all bindings matched together with their respective sites
                    var sites = iisManager.Sites.
                        AsEnumerable().
                        Where(s => s.State == ObjectState.Started);

                    // Option: hide http bindings when there are already https equivalents
                    if (Program.Options.HideHttps)
                    {
                        sites = sites.Where(site => site.Bindings.
                            Where(binding => binding.Protocol == "http").
                            Any(binding => !site.Bindings.Any(other => other.Protocol == "https" && other.Host == binding.Host)));
                    }

                    var targets = sites.
                        Select(site => new Target
                        {
                            SiteId = site.Id,
                            Host = site.Name,
                            WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                            PluginName = Name,
                            AlternativeNames = site.Bindings.Select(x => x.Host).
                                                    Where(x => !string.IsNullOrWhiteSpace(x)).
                                                    Select(x => _idnMapping.GetAscii(x)).
                                                    Distinct().
                                                    ToList()
                        }).
                        Where(target =>
                        {
                            if (target.AlternativeNames.Count > Settings.maxNames)
                            {
                                Program.Log.Information("{site} has too many hosts for a single certificate. Let's Encrypt has a maximum of {maxNames}.", target.Host, Settings.maxNames);
                                return false;
                            }
                            else if (target.AlternativeNames.Count == 0)
                            {
                                Program.Log.Information("No valid hosts found for {site}.", target.Host);
                                return false;
                            }
                            return true;
                        }).
                        OrderBy(target => target.SiteId).
                        ToList();

                    if (targets.Count() == 0)
                    {
                        Program.Log.Warning("No applicable IIS sites were found.");
                    }
                    return targets;
                }
            }
            return new List<Target>();
        }

        private readonly string _sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            var directory = Path.GetDirectoryName(answerPath);
            var webConfigPath = Path.Combine(directory, "web.config");

            Program.Log.Debug("Writing web.config to {webConfigPath}", webConfigPath);
            File.Copy(_sourceFilePath, webConfigPath, true);
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
                hosts.AddRange(target.AlternativeNames);
                hosts = hosts.
                    Where(x => !string.IsNullOrWhiteSpace(x)).
                    Select((x) => _idnMapping.GetUnicode(x)).
                    Distinct().
                    ToList();
                
                foreach (var host in hosts)
                {
                    var existingBinding =
                        (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b).FirstOrDefault();
                    if (existingBinding != null)
                    {
                        Program.Log.Information(true, "Updating existing https binding for {host}", host);
                        Program.Log.Information("IIS will serve the new certificate after the Application Pool IdleTimeout has been reached.");

                        // Replace instead of change binding because of #371
                        Binding replacement = site.Bindings.CreateElement("binding");
                        replacement.Protocol = existingBinding.Protocol;
                        replacement.BindingInformation = existingBinding.BindingInformation;
                        replacement.CertificateStoreName = store.Name;
                        replacement.CertificateHash = certificate.GetCertHash();
                        foreach (ConfigurationAttribute attr in existingBinding.Attributes)
                        {
                            replacement.SetAttributeValue(attr.Name, attr.Value);
                        }
                        site.Bindings.Remove(existingBinding);
                        site.Bindings.Add(replacement);
                    }
                    else
                    {
                        Program.Log.Information(true, "Adding new https binding for {host}", host);
                        var existingHTTPBinding =
                            (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b)
                                .FirstOrDefault();
                        if (existingHTTPBinding != null)
                        {
                            string IP = GetIP(existingHTTPBinding.EndPoint.ToString(), host);
                            Binding iisBinding = site.Bindings.CreateElement("binding");
                            iisBinding.Protocol = "https";
                            iisBinding.BindingInformation = IP + ":443:" + host;
                            iisBinding.CertificateStoreName = store.Name;
                            iisBinding.CertificateHash = certificate.GetCertHash();
                            if (_iisVersion.Major >= 8)
                            {
                                iisBinding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                            }
                            site.Bindings.Add(iisBinding);
                        }
                        else
                        {
                            Program.Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                        }
                    }
                }
                Program.Log.Information("Committing binding changes to IIS");
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
                    hosts = hosts.
                        Where(x => !string.IsNullOrWhiteSpace(x)).
                        Select((x) => _idnMapping.GetUnicode(x)).
                        Distinct().
                        ToList();

                    foreach (var host in hosts)
                    {
                        var existingBinding =
                            (from b in site.Bindings where b.Host == host && b.Protocol == "https" select b)
                                .FirstOrDefault();
                        if (!(_iisVersion.Major >= 8))
                        {
                            var errorMessage = "You aren't using IIS 8 or greater, so centralized SSL is not supported";
                            Program.Log.Error(errorMessage);
                            //Not using IIS 8+ so can't set centralized certificates
                            throw new InvalidOperationException(errorMessage);
                        }
                        else if (existingBinding != null)
                        {
                            if (existingBinding.GetAttributeValue("sslFlags").ToString() != "3")
                            {
                                Program.Log.Information("Updating existing https binding");
                                //IIS 8+ and not using centralized SSL with SNI
                                existingBinding.CertificateStoreName = null;
                                existingBinding.CertificateHash = null;
                                existingBinding.SetAttributeValue("sslFlags", 3);
                            }
                            else
                            {
                                Program.Log.Information("You specified Central SSL, have an existing binding using Central SSL with SNI, so there is nothing to update for this binding");
                            }
                        }
                        else
                        {
                            Program.Log.Information(true, "Adding Central SSL https binding");
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
                                Program.Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                            }
                        }
                    }
                    Program.Log.Information("Committing binding changes to IIS");
                    iisManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Program.Log.Error("Error setting binding {@ex}", ex);
                throw new InvalidProgramException(ex.Message);
            }
        }

        private static Version GetIisVersion()
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
            Auto(target);
        }


        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Program.Log.Debug("Deleting answer");
            try
            {
                var answerFileInfo = new FileInfo(answerPath);
                if (answerFileInfo.Exists)
                {
                    answerFileInfo.Delete();
                }

                if (Properties.Settings.Default.CleanupFolders == true)
                {
                    var answerDirectoryInfo = answerFileInfo.Directory;
                    var children = answerDirectoryInfo.GetFileSystemInfos();
                    if (children.Length == 1)
                    {
                        if (children.First().Name.ToLower() == "web.config")
                        {
                            Program.Log.Debug("Deleting web.config");
                            children.First().Delete();
                            Program.Log.Debug("Deleting {folderPath}", answerDirectoryInfo.FullName);
                            answerDirectoryInfo.Delete();

                            var filePathFirstDirectory = answerDirectoryInfo.Parent;
                            if (filePathFirstDirectory.GetFileSystemInfos().Length == 0)
                            {
                                Program.Log.Debug("Deleting {filePathFirstDirectory}", filePathFirstDirectory.FullName);
                                filePathFirstDirectory.Delete();
                            }
                            else
                            {
                                Program.Log.Debug("Additional files or folders exist in {folderPath}, not deleting.", filePathFirstDirectory.FullName);
                            }
                        }
                        else
                        {
                            Program.Log.Debug("Unexpected file discovered in {folderPath}, not deleting.", answerDirectoryInfo.FullName);
                        }
                    }
                    else
                    {
                        Program.Log.Debug("Additional files or folders exist in {folderPath} not deleting.", answerDirectoryInfo.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Program.Log.Debug("Writing challenge answer to {answerPath}", answerPath);
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

        public override ScheduledRenewal Refresh(ScheduledRenewal renewal)
        {
            // Web root path may have changed since the initial creation of the certificate, get current path from IIS
            var san = string.Equals(renewal.San, "true", StringComparison.InvariantCultureIgnoreCase);
            Target match = null;
            if (san)
            {
                match = GetSites().FirstOrDefault(binding => binding.SiteId == renewal.Binding.SiteId);
            }
            else
            {
                match = GetTargets().FirstOrDefault(binding => binding.Host == renewal.Binding.Host);
            }

            if (match != null)
            {
                // Update values based on found match
                Program.Log.Information("Target for {renewal} still found in IIS, updating records", renewal.Binding.Host);

                // Update web root path
                if (!string.Equals(renewal.Binding.WebRootPath, match.WebRootPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    Program.Log.Warning("- Change WebRootPath from {old} to {new}", renewal.Binding.WebRootPath, match.WebRootPath);
                    renewal.Binding.WebRootPath = match.WebRootPath;
                }

                // Add/remove alternative names
                var addedNames = match.AlternativeNames.Except(renewal.Binding.AlternativeNames);
                var removedNames = renewal.Binding.AlternativeNames.Except(match.AlternativeNames);
                if (addedNames.Count() > 0)
                {
                    Program.Log.Warning("- Added host(s): {names}", string.Join(", ", addedNames));
                }
                if (removedNames.Count() > 0)
                {
                    Program.Log.Warning("- Removed host(s): {names}", string.Join(", ", removedNames));
                }
                renewal.Binding.AlternativeNames = match.AlternativeNames;

                // Return updated binding
                return renewal;
            }
            else
            {
                // No match, return nothing, effectively cancelling the renewal
                Program.Log.Error("Target for {renewal} no longer found in IIS, cancelling renewal", renewal);
                return null;
            }
        }
    }
}