using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.JOSE;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using LetsEncrypt.ACME.Simple.Core.Schedules;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Services
{
    public class CertificateService : ICertificateService
    {
        protected IOptions Options;
        protected IConsoleService ConsoleService;
        public CertificateService(IOptions options, IConsoleService consoleService)
        {
            Options = options;
            ConsoleService = consoleService;
        }

        public void InstallCertificate(Target binding, string pfxFilename, out X509Store store,
            out X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(Options.CertificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            Log.Information("Opened Certificate Store {Name}", store.Name);
            certificate = null;
            try
            {
                var flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (Properties.Settings.Default.PrivateKeyExportable)
                {
                    Log.Information("Set private key exportable");
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                var password = string.IsNullOrWhiteSpace(binding.PfxPassword)
                    ? Properties.Settings.Default.PFXPassword
                    : binding.PfxPassword;

                certificate = new X509Certificate2(pfxFilename, password, flags)
                {
                    FriendlyName = $"{binding.Host} {DateTime.Now.ToString(Properties.Settings.Default.FileDateFormat)}"
                };

                Log.Debug("{FriendlyName}", certificate.FriendlyName);
                Log.Information("Adding Certificate to Store");

                store.Add(certificate);

                Log.Information("Closing Certificate Store");
            }
            catch (Exception ex)
            {
                Log.Error("Error saving certificate {@ex}", ex);
            }
            store.Close();
        }

        public void UninstallCertificate(string host, out X509Store store, X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(Options.CertificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            Log.Information("Opened Certificate Store {Name}", store.Name);

            try
            {
                var certificateCollection = store.Certificates.Find(X509FindType.FindBySubjectName, host, false);

                foreach (var cert in certificateCollection)
                {
                    var subjectName = cert.Subject.Split(',');

                    if (cert.FriendlyName == certificate.FriendlyName || subjectName[0] != "CN=" + host)
                        continue;

                    Log.Information("Removing Certificate from Store {@cert}", cert);
                    store.Remove(cert);
                }

                Log.Information("Closing Certificate Store");
            }
            catch (Exception ex)
            {
                Log.Error("Error removing certificate {@ex}", ex);
            }

            store.Close();
        }
        
        public void GetCertificateForTargetId(List<Target> targets, int targetId)
        {
            if (!Options.San)
            {
                var targetIndex = targetId - 1;
                if (targetIndex < 0 || targetIndex >= targets.Count)
                    return;

                var binding = GetBindingByIndex(targets, targetIndex);
                var plugin = Options.Plugins[binding.PluginName];
                plugin.Auto(binding);
            }
            else
            {
                var binding = GetBindingBySiteId(targets, targetId);
                var plugin = Options.Plugins[binding.PluginName];
                plugin.Auto(binding);
            }
        }

        private static Target GetBindingByIndex(List<Target> targets, int targetIndex)
        {
            return targets[targetIndex];
        }

        private static Target GetBindingBySiteId(List<Target> targets, int targetId)
        {
            return targets.First(t => t.SiteId == targetId);
        }
        
        public void GetCertificatesForAllHosts(List<Target> targets)
        {
            foreach (var target in targets)
            {
                var plugin = Options.Plugins[target.PluginName];
                plugin.Auto(target);
            }
        }

        public void LoadSignerFromFile(RS256Signer signer, string signerPath)
        {
            Log.Information("Loading Signer from {signerPath}", signerPath);
            using (var signerStream = File.OpenRead(signerPath))
                signer.Load(signerStream);
        }
        
        public void CheckRenewalsAndWaitForEnterKey()
        {
            CheckRenewals();
            WaitForEnterKey();
        }

        private void WaitForEnterKey()
        {
#if DEBUG
            ConsoleService.PromptEnter();
#endif
        }

        public void CheckRenewals()
        {
            Log.Information("Checking Renewals");

            var renewals = Options.Settings.LoadRenewals();
            if (renewals.Count == 0)
                Log.Information("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
                ProcessRenewal(renewals, now, renewal);
        }

        public void ProcessRenewal(List<ScheduledRenewal> renewals, DateTime now, ScheduledRenewal renewal)
        {
            Log.Information("Checking {renewal}", renewal);
            if (renewal.Date >= now) return;

            Log.Information("Renewing certificate for {renewal}", renewal);
            if (string.IsNullOrWhiteSpace(renewal.CentralSsl))
            {
                //Not using Central SSL
                Options.CentralSsl = false;
                Options.CentralSslStore = null;
            }
            else
            {
                //Using Central SSL
                Options.CentralSsl = true;
                Options.CentralSslStore = renewal.CentralSsl;
            }

            if (string.IsNullOrWhiteSpace(renewal.San))
            {
                //Not using San
                Options.San = false;
            }
            else if (renewal.San.ToLower() == "true")
            {
                //Using San
                Options.San = true;
            }
            else
            {
                //Not using San
                Options.San = false;
            }

            if (string.IsNullOrWhiteSpace(renewal.KeepExisting))
            {
                //Not using KeepExisting
                Options.KeepExisting = false;
            }
            else if (renewal.KeepExisting.ToLower() == "true")
            {
                //Using KeepExisting
                Options.KeepExisting = true;
            }
            else
            {
                //Not using KeepExisting
                Options.KeepExisting = false;
            }

            if (!string.IsNullOrWhiteSpace(renewal.Script))
            {
                Options.Script = renewal.Script;
            }

            if (!string.IsNullOrWhiteSpace(renewal.ScriptParameters))
            {
                Options.ScriptParameters = renewal.ScriptParameters;
            }

            if (renewal.Warmup)
            {
                Options.Warmup = true;
            }

            var plugin = Options.Plugins[renewal.Binding.PluginName];
            plugin.Renew(renewal.Binding);

            renewal.Date = DateTime.UtcNow.AddDays(Options.RenewalPeriodDays);
            Options.Settings.SaveRenewals(renewals);

            Log.Information("Renewal Scheduled {renewal}", renewal);
        }

        public void ProcessDefaultCommand(List<Target> targets, string command)
        {
            if (int.TryParse(command, out int targetId))
            {
                GetCertificateForTargetId(targets, targetId);
                return;
            }

            ConsoleService.HandleMenuResponseForPlugins(targets, command);
        }
    }
}
