using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class IISClient : Plugin
    {
        public Version Version = GetIISVersion();
        public IdnMapping IdnMapping = new IdnMapping();
        public const string PluginName = "IIS";
        public override string Name => PluginName;
        public enum SSLFlags
        {
            SNI = 1,
            CentralSSL = 2
        }

        public ServerManager GetServerManager()
        {
            if (_ServerManager == null)
            {
                if (Version.Major > 0)
                {
                    _ServerManager = new ServerManager();
                }
            }
            return _ServerManager;
        }
        private ServerManager _ServerManager;

        internal void UnlockSection(string path)
        {
            // Unlock handler section
            var config = GetServerManager().GetApplicationHostConfiguration();
            var section = config.GetSection(path);
            if (section.OverrideModeEffective == OverrideMode.Deny)
            {
                section.OverrideMode = OverrideMode.Allow;
                GetServerManager().CommitChanges();
                Program.Log.Warning("Unlocked section {section}", path);
            }
        }

        /// <summary>
        /// Install for regular bindings
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pfxFilename"></param>
        /// <param name="store"></param>
        /// <param name="certificate"></param>
        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            SSLFlags flags = 0;
            if (Version.Major >= 8) {
                flags = SSLFlags.SNI;
            }
            var site = GetSite(target);
            var hosts = target.GetHosts(true);
            foreach (var host in hosts) {
                AddOrUpdateBinding(site, host, flags, certificate.GetCertHash(), store.Name, Program.Options.SSLPort);
            }
            Program.Log.Information("Committing binding changes to IIS");
            GetServerManager().CommitChanges();
            Program.Log.Information("IIS will serve the new certificate after the Application Pool IdleTimeout has been reached.");
        }

        /// <summary>
        /// Install for Central SSL bindings
        /// </summary>
        /// <param name="target"></param>
        public override void Install(Target target)
        {
            if (Version.Major < 8) {
                var errorMessage = "You aren't using IIS 8 or greater, so Centralized SSL is not supported";
                Program.Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            try {
                var site = GetSite(target);
                var hosts = target.GetHosts(true);
                foreach (var host in hosts) {
                    AddOrUpdateBinding(site, host, SSLFlags.SNI | SSLFlags.CentralSSL, null, null, Program.Options.SSLPort);
                }
                Program.Log.Information("Committing binding changes to IIS");
                GetServerManager().CommitChanges();
            } catch (Exception ex) {
                Program.Log.Error("Error setting binding {@ex}", ex);
                throw new InvalidProgramException(ex.Message);
            }
        }

        public void AddOrUpdateBinding(Site site, string host, SSLFlags flags, byte[] thumbprint, string store, int newPort = 443)
        {
            var existingBindings = site.Bindings.Where(x => string.Equals(x.Host, host, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var existingHttpsBindings = existingBindings.Where(x => x.Protocol == "https").ToList();
            var existingHttpBindings = existingBindings.Where(x => x.Protocol == "http").ToList();
            var update = existingHttpsBindings.Any();
            if (update)
            {
                // Already on HTTPS, update those bindings
                foreach (var existingBinding in existingHttpsBindings)
                {
                    var currentFlags = int.Parse(existingBinding.GetAttributeValue("sslFlags").ToString());
                    if (currentFlags == (int)flags &&
                        StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, thumbprint) &&
                        string.Equals(existingBinding.CertificateStoreName, store, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Program.Log.Verbose("No binding update needed");
                    }
                    else
                    {
                        Program.Log.Information(true, "Updating existing https binding {host}:{port}", host, existingBinding.EndPoint.Port);

                        // Replace instead of change binding because of #371
                        Binding replacement = site.Bindings.CreateElement("binding");
                        replacement.Protocol = existingBinding.Protocol;
                        replacement.BindingInformation = existingBinding.BindingInformation;
                        replacement.CertificateStoreName = store;
                        replacement.CertificateHash = thumbprint;
                        foreach (ConfigurationAttribute attr in existingBinding.Attributes)
                        {
                            replacement.SetAttributeValue(attr.Name, attr.Value);
                        }
                        replacement.SetAttributeValue("sslFlags", flags);
                        site.Bindings.Remove(existingBinding);
                        site.Bindings.Add(replacement);
                    }
                }
            }
            else
            {
                Program.Log.Information(true, "Adding new https binding");
                string IP = "*";
                if (existingHttpBindings.Any()) {
                    IP = GetIP(existingHttpBindings.First().EndPoint.ToString(), host);
                } else {
                    Program.Log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                }
                Binding newBinding = site.Bindings.CreateElement("binding");
                newBinding.Protocol = "https";
                newBinding.BindingInformation = $"{IP}:{newPort}:{host}";
                newBinding.CertificateStoreName = store;
                newBinding.CertificateHash = thumbprint;
                newBinding.SetAttributeValue("sslFlags", flags);
                site.Bindings.Add(newBinding);
            }
        }

        private static Version GetIISVersion()
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

        private Site GetSite(Target target)
        {
            foreach (var site in GetServerManager().Sites)
            {
                if (site.Id == target.SiteId) return site;
            }
            throw new Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }

        private string GetIP(string HTTPEndpoint, string host)
        {
            string IP = "*";
            string HTTPIP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'),
                (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

            if (Version.Major >= 8 && HTTPIP != "0.0.0.0")
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

        internal Target UpdateWebRoot(Target saved, Target match)
        {
            // Update web root path
            if (!string.Equals(saved.WebRootPath, match.WebRootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                Program.Log.Warning("- Change WebRootPath from {old} to {new}", saved.WebRootPath, match.WebRootPath);
                saved.WebRootPath = match.WebRootPath;
            }
            return saved;
        }

        internal Target UpdateAlternativeNames(Target saved, Target match)
        {
            // Add/remove alternative names
            var addedNames = match.AlternativeNames.Except(saved.AlternativeNames).Except(saved.GetExcludedHosts());
            var removedNames = saved.AlternativeNames.Except(match.AlternativeNames);
            if (addedNames.Count() > 0)
            {
                Program.Log.Warning("- Added host(s): {names}", string.Join(", ", addedNames));
            }
            if (removedNames.Count() > 0)
            {
                Program.Log.Warning("- Removed host(s): {names}", string.Join(", ", removedNames));
            }
            saved.AlternativeNames = match.AlternativeNames;
            return saved;
        }
    }
}