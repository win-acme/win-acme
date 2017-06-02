using letsencrypt.Support;
using Microsoft.Web.Administration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace letsencrypt
{
    public class IISPlugin : Plugin
    {
        public override string Name => R.IIS;

        public override bool RequiresElevated => true;

        private static Version _iisVersion = null;

        private List<Target> targets;

        protected Dictionary<string, string> config;

        private static Type _managerType = typeof(IISServerManagerWrapper);

        public override void PrintMenu()
        {
            Console.WriteLine(R.IISMenuOption);
        }

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.I;

        public override bool Validate(Options options)
        {
            if (GetIisVersion().Major == 0)
            {
                Log.Information(R.IISVersionnotfoundinwindowsregistry);
                return false;
            }
            else
            {
                var targets = GetTargets(options);
                if (targets.Count == 0)
                {
                    Log.Information(R.NoIISbindingswithhostnameswerefound);
                    return false;
                }
            }
            config = GetConfig(options);
            return true;
        }

        public override bool SelectOptions(Options options)
        {
            targets = GetTargets(options);
            string hostNames = LetsEncrypt.GetString(config, "host_name");
            if (string.IsNullOrEmpty(hostNames))
            {
                Console.WriteLine(R.GetcertificatesforallhostsMenu);
                Console.WriteLine(R.QuitMenu);
                Console.Write(R.Choosefromoneofthemenuoptionsabove);
                var command = LetsEncrypt.ReadCharFromConsole(options);
                switch (command)
                {
                    case ConsoleKey.A:
                        break;
                    case ConsoleKey.Q:
                        return false;
                    default:
                        GetTargetsForEntry(options, command.ToString().ToLowerInvariant());
                        break;
                }
            }
            else
            {
                GetTargetsForHostNames(options, hostNames.Split(',', ';', ' '));
            }
            return true;
        }

        private void GetTargetsForEntry(Options options, string command)
        {
            var targetId = 0;
            if (int.TryParse(command, out targetId))
            {
                if (!options.San)
                {
                    var targetIndex = targetId - 1;
                    if (targetIndex >= 0 && targetIndex < targets.Count)
                    {
                        targets = new List<Target>(new[] { targets[targetIndex] });
                    }
                }
                else
                {
                    targets = new List<Target>(new[] { targets.First(t => t.SiteId == targetId) });
                }
            }
        }

        private void GetTargetsForHostNames(Options options, string[] hostNames)
        {
            targets = targets.Where(t => hostNames.Contains(t.Host)).ToList();
        }

        public override void Install(Target target, Options options)
        {
            Auto(target, options);
        }

        public override string Auto(Target target, Options options)
        {
            string pfxFilename = base.Auto(target, options);

            if (options.Test && !options.Renew)
            {
                if (!LetsEncrypt.PromptYesNo(options, R.DoyouwanttoinstallthecertificateintotheCertificateStore))
                {
                    return pfxFilename;
                }
            }

            if (!options.CentralSsl)
            {
                X509Store store;
                X509Certificate2 certificate;
                Log.Information(R.InstallingSSLcertificateinthecertificatestore);
                InstallCertificate(target, pfxFilename, options, out store, out certificate);
                if (options.Test && !options.Renew)
                {
                    if (!LetsEncrypt.PromptYesNo(options, R.DoyouwanttoupdatethecertificateinIIS))
                    {
                        return pfxFilename;
                    }
                }
                Log.Information(R.InstallingSSLcertificateinIIS);
                InstallSSL(target, pfxFilename, store, certificate, options);
                if (!options.KeepExisting)
                {
                    UninstallCertificate(target.Host, certificate, options);
                }
            }
            else if (!options.Renew || !options.KeepExisting)
            {
                InstallCentralSSL(target, options);
            }
            return pfxFilename;
        }

        internal static void InstallCertificate(Target binding, string pfxFilename, Options options, out X509Store store, out X509Certificate2 certificate)
        {
            store = OpenCertificateStore(options);

            Log.Information(R.Openedcertificatestore, store.Name);
            certificate = null;
            try
            {
                X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (options.PrivateKeyExportable)
                {
                    Log.Information(R.Setprivatekeyexportable);
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                certificate = new X509Certificate2(pfxFilename, options.PFXPassword,
                    flags);

                certificate.FriendlyName =
                    $"{binding.Host} {DateTime.Now.ToString(options.FileDateFormat)}";
                Log.Debug("{FriendlyName}", certificate.FriendlyName);

                Log.Information(R.Addingcertificatetostore);
                store.Add(certificate);

                Log.Information(R.Closingcertificatestore);
            }
            catch (Exception ex)
            {
                Log.Error(R.Errorsavingcertificate, ex);
            }
            store.Close();
        }

        protected static X509Store OpenCertificateStore(Options options)
        {
            X509Store store;
            try
            {
                store = new X509Store(options.CertificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error(R.Errorencounteredwhileopeningcertificatestore, ex);
                throw;
            }
            return store;
        }

        internal void UninstallCertificate(string host, X509Certificate2 certificate, Options options)
        {
            X509Store store;
            try
            {
                store = new X509Store(options.CertificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error(R.Errorencounteredwhileopeningcertificatestore, ex);
                throw;
            }

            Log.Information(R.Openedcertificatestore, store.Name);
            try
            {
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindBySubjectName, host, false);

                foreach (var cert in col)
                {
                    var subjectName = cert.Subject.Split(',');

                    if (cert.FriendlyName != certificate.FriendlyName && subjectName[0] == "CN=" + host)
                    {
                        Log.Information(R.Removingcertificatefromstore, cert);
                        store.Remove(cert);
                    }
                }

                Log.Information(R.Closingcertificatestore);
            }
            catch (Exception ex)
            {
                Log.Error(R.Errorremovingcertificate, ex);
            }
            store.Close();
        }

        public override List<Target> GetTargets(Options options)
        {
            Log.Information(R.ScanningIISsitebindingsforhosts);

            if (targets != null) { return targets; }

            var result = new List<Target>();
            
            if (GetIisVersion().Major == 0)
            {
                Log.Information(R.IISVersionnotfoundinwindowsregistry);
            }
            else
            {
                using (var iisManager = GetServerManager())
                {
                    foreach (var site in iisManager.Sites)
                    {
                        List<Target> returnHTTP = new List<Target>();
                        List<Target> siteHTTPS = new List<Target>();
                        List<Target> siteHTTP = new List<Target>();

                        foreach (var binding in site.Bindings)
                        {
                            //Get HTTP sites that aren't IDN
                            if (!string.IsNullOrEmpty(binding.Host) && binding.Protocol == "http" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (returnHTTP.Where(h => h.Host == binding.Host).Count() == 0)
                                {
                                    returnHTTP.Add(new Target()
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.GetPhysicalPath(),
                                        PluginName = Name
                                    });
                                }
                            }
                            //Get HTTPS sites that aren't IDN
                            if (!string.IsNullOrEmpty(binding.Host) && binding.Protocol == "https" &&
                                !Regex.IsMatch(binding.Host, @"[^\u0000-\u007F]"))
                            {
                                if (siteHTTPS.Where(h => h.Host == binding.Host).Count() == 0)
                                {
                                    siteHTTPS.Add(new Target()
                                    {
                                        SiteId = site.Id,
                                        Host = binding.Host,
                                        WebRootPath = site.GetPhysicalPath(),
                                        PluginName = Name
                                    });
                                }
                            }
                        }

                        siteHTTP.AddRange(returnHTTP);
                        if (options.HideHttps)
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
                    Log.Information(R.NoIISbindingswithhostnameswerefound);
                }
            }

            return result;
        }

        private static IIISServerManager GetServerManager()
        {
            return _managerType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes) as IIISServerManager;
        }

        public static void RegisterServerManager<T>() where T : IIISServerManager
        {
            _managerType = typeof(T);
        }

        internal List<Target> GetSites()
        {
            Log.Information(R.ScanningIISsites);

            var result = new List<Target>();
            
            if (GetIisVersion().Major == 0)
            {
                Log.Information(R.IISVersionnotfoundinwindowsregistry);
            }
            else
            {
                using (var iisManager = GetServerManager())
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
                                    WebRootPath = site.GetPhysicalPath(),
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
                                WebRootPath = site.GetPhysicalPath(),
                                PluginName = Name,
                                AlternativeNames = hosts
                            });
                        }
                        else if (hosts.Count > 0)
                        {
                            Log.Error(R.NamehastoomanyhostsforaSancertificate, site.Name);
                        }
                    }
                }

                if (result.Count == 0)
                {
                    Log.Information(R.NoIISbindingswithhostnameswerefound);
                }
            }

            return result.OrderBy(r => r.SiteId).ToList();
        }

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            var directory = Path.GetDirectoryName(answerPath);
            var webConfigPath = Path.Combine(directory, "web.config");
            
            Log.Information(R.WritingWebConfig, webConfigPath);
            File.Copy(Path.Combine(BaseDirectory, "web_config.xml"), webConfigPath, true);
        }

        public void InstallSSL(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate, Options options)
        {
            using (var iisManager = GetServerManager())
            {
                var site = GetSite(target, iisManager);
                List<string> hosts = new List<string>();
                if (!options.San)
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
                        Log.Information(R.UpdatingexistingSSLbinding);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Log.Information(R.IISCertificateTakesEffectAfterAppPoolRecycle);
                        Console.ResetColor();

                        existingBinding.CertificateStoreName = store.Name;
                        existingBinding.CertificateHash = certificate.GetCertHash();
                    }
                    else
                    {
                        Log.Information(R.AddingSSLbinding);
                        var existingHTTPBinding =
                            (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b)
                                .FirstOrDefault();
                        if (existingHTTPBinding != null)
                        {
                            string IP = GetIP(existingHTTPBinding.GetEndPoint(), host, options);

                            var iisBinding = site.AddBinding(IP + ":443:" + host, certificate.GetCertHash(), store.Name);
                            iisBinding.Protocol = "https";

                            if (GetIisVersion().Major >= 8)
                            { 
                                iisBinding.SetAttributeValue("sslFlags", 1); // Enable SNI support
                            }
                        }
                        else
                        {
                            Log.Warning(R.NoHTTPbindingforhostonsitename, host, site.Name);
                        }
                    }
                }
                Log.Information(R.CommittingbindingchangestoIIS);
                iisManager.CommitChanges();
            }
        }

        public void InstallCentralSSL(Target target, Options options)
        {
            //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
            Log.Information(R.InstallingcentralSSLcertificate);
            try
            {
                using (var iisManager = GetServerManager())
                {
                    var site = GetSite(target, iisManager);

                    List<string> hosts = new List<string>();
                    if (!options.San)
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
                        if (!(GetIisVersion().Major >= 8))
                        {
                            var errorMessage = R.CentralizedSSLisonlysupportedonIIS8orgreater;
                            Log.Error(errorMessage);
                            //Not using IIS 8+ so can't set centralized certificates
                            throw new InvalidOperationException(errorMessage);
                        }
                        else if (existingBinding != null)
                        {
                            if (existingBinding.GetAttributeValue("sslFlags").ToString() != "3")
                            {
                                Log.Information(R.UpdatingexistingSSLbinding);
                                //IIS 8+ and not using centralized SSL with SNI
                                existingBinding.CertificateStoreName = null;
                                existingBinding.CertificateHash = null;
                                existingBinding.SetAttributeValue("sslFlags", 3);
                            }
                            else
                            {
                                Log.Information(R.CentralSSLExistingBindingWithSNI);
                            }
                        }
                        else
                        {
                            Log.Information(R.AddingcentralSSLbinding);
                            var existingHTTPBinding =
                                (from b in site.Bindings where b.Host == host && b.Protocol == "http" select b)
                                    .FirstOrDefault();
                            if (existingHTTPBinding != null)
                            //This had been a fix for the multiple site San cert, now it's a precaution against erroring out
                            {
                                string IP = GetIP(existingHTTPBinding.GetEndPoint(), host, options);

                                var iisBinding = site.AddBinding(IP + ":443:" + host, "https");

                                iisBinding.SetAttributeValue("sslFlags", 3);
                                // Enable Centralized Certificate Store with SNI
                            }
                            else
                            {
                                Log.Warning(R.NoHTTPbindingforhostonsitename, host, site.Name);
                            }
                        }
                    }
                    Log.Information(R.CommittingbindingchangestoIIS);
                    iisManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error(R.Errorsettingbinding, ex);
                throw new InvalidProgramException(ex.Message);
            }
        }

        public Version GetIisVersion()
        {
            if (_iisVersion == null)
            {
                _iisVersion = new Version(0, 0);
                try
                {
                    _iisVersion = GetServerManager().GetVersion();
                }
                catch(Exception e)
                {
                    Log.Error(R.ErrorreadingtheIISversion);
                    Log.Error(e.Message);
                }
            }
            return _iisVersion;
        }

        public override void Renew(Target target, Options options)
        {
            Auto(target, options);
        }

        public override void DeleteAuthorization(Options options, string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information(R.Deletinganswer);
            File.Delete(answerPath);

            try
            {
                if (options.CleanupFolders)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
                    var files = Directory.GetFiles(folderPath);

                    if (files.Length == 1)
                    {
                        if (files[0] == (folderPath + "web.config"))
                        {
                            Log.Information(R.Deletingwebconfig);
                            File.Delete(files[0]);
                            Log.Information(R.Deletingfolderpath, folderPath);
                            Directory.Delete(folderPath);

                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Log.Information(R.Deletingfolderpath, filePathFirstDirectory);
                            Directory.Delete(filePathFirstDirectory);
                        }
                        else
                        {
                            Log.Warning(R.Additionalfilesexistinfolderpath, folderPath);
                        }
                    }
                    else
                    {
                        Log.Warning(R.Additionalfilesexistinfolderpath, folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(R.Erroroccuredwhiledeletingfolderstructure, ex);
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information(R.WritingchallengeanswertoanswerPath, answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }

        private IIISSite GetSite(Target target, IIISServerManager iisManager)
        {
            foreach (var site in iisManager.Sites)
            {
                if (site.Id == target.SiteId)
                    return site;
            }
            throw new System.Exception(string.Format(R.UnabletofindIISsiteID, target.SiteId));
        }

        private string GetIP(string HTTPEndpoint, string host, Options options)
        {
            string IP = "*";
            string HTTPIP = HTTPEndpoint.Remove(HTTPEndpoint.IndexOf(':'),
                (HTTPEndpoint.Length - HTTPEndpoint.IndexOf(':')));

            if (GetIisVersion().Major >= 8 && HTTPIP != "0.0.0.0")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format(R.WarningcreatingSSLbindingforhost, host));
                Console.ResetColor();
                Console.WriteLine(R.TheHTTPbindingisIPspecific);
                Console.WriteLine(R.YouneedtoeditthebindingturnoffSNI);
                Console.WriteLine(R.OtherwisemanuallycreatetheHTTPSbinding);
                Console.WriteLine(string.Format(R.Seelinkformoreinformation, "https://github.com/Lone-Coder/letsencrypt-win-simple/wiki/HTTPS-Binding-With-Specific-IP"));
                Console.WriteLine();
                if(LetsEncrypt.PromptYesNo(options, R.PressYtocontinueoranyotherkeytostopinstalling, false))
                {
                    IP = HTTPIP;
                }
                else
                {
                    throw new Exception(R.HTTPSbindingnotcreatedduetoHTTPbindinghavingspecificIP);
                }
            }
            else if (HTTPIP != "0.0.0.0")
            {
                IP = HTTPIP;
            }
            return IP;
        }
    }
}