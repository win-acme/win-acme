using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.JOSE;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Schedules;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Services
{
    public class CertificateService
    {
        public void InstallCertificate(Target binding, string pfxFilename, out X509Store store,
            out X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(App.Options.CertificateStore, StoreLocation.LocalMachine);
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
                X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (Properties.Settings.Default.PrivateKeyExportable)
                {
                    Console.WriteLine($" Set private key exportable");
                    Log.Information("Set private key exportable");
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                certificate = new X509Certificate2(pfxFilename, Properties.Settings.Default.PFXPassword,
                    flags);

                certificate.FriendlyName =
                    $"{binding.Host} {DateTime.Now.ToString(Properties.Settings.Default.FileDateFormat)}";
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
                store = new X509Store(App.Options.CertificateStore, StoreLocation.LocalMachine);
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
                X509Certificate2Collection col = store.Certificates.Find(X509FindType.FindBySubjectName, host, false);

                foreach (var cert in col)
                {
                    var subjectName = cert.Subject.Split(',');

                    if (cert.FriendlyName != certificate.FriendlyName && subjectName[0] == "CN=" + host)
                    {
                        Log.Information("Removing Certificate from Store {@cert}", cert);
                        store.Remove(cert);
                    }
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
            if (!App.Options.San)
            {
                var targetIndex = targetId - 1;
                if (targetIndex >= 0 && targetIndex < targets.Count)
                {
                    Target binding = GetBindingByIndex(targets, targetIndex);
                    binding.Plugin.Auto(binding);
                }
            }
            else
            {
                Target binding = GetBindingBySiteId(targets, targetId);
                binding.Plugin.Auto(binding);
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
                target.Plugin.Auto(target);
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

        private static void WaitForEnterKey()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
#endif
        }

        public void CheckRenewals()
        {
            Log.Information("Checking Renewals");

            var renewals = App.Options.Settings.LoadRenewals();
            if (renewals.Count == 0)
                Log.Information("No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
                ProcessRenewal(renewals, now, renewal);
        }

        private void ProcessRenewal(List<ScheduledRenewal> renewals, DateTime now, ScheduledRenewal renewal)
        {
            Log.Information("Checking {renewal}", renewal);
            if (renewal.Date >= now) return;

            Log.Information("Renewing certificate for {renewal}", renewal);
            if (string.IsNullOrWhiteSpace(renewal.CentralSsl))
            {
                //Not using Central SSL
                App.Options.CentralSsl = false;
                App.Options.CentralSslStore = null;
            }
            else
            {
                //Using Central SSL
                App.Options.CentralSsl = true;
                App.Options.CentralSslStore = renewal.CentralSsl;
            }
            if (string.IsNullOrWhiteSpace(renewal.San))
            {
                //Not using San
                App.Options.San = false;
            }
            else if (renewal.San.ToLower() == "true")
            {
                //Using San
                App.Options.San = true;
            }
            else
            {
                //Not using San
                App.Options.San = false;
            }
            if (string.IsNullOrWhiteSpace(renewal.KeepExisting))
            {
                //Not using KeepExisting
                App.Options.KeepExisting = false;
            }
            else if (renewal.KeepExisting.ToLower() == "true")
            {
                //Using KeepExisting
                App.Options.KeepExisting = true;
            }
            else
            {
                //Not using KeepExisting
                App.Options.KeepExisting = false;
            }
            if (!string.IsNullOrWhiteSpace(renewal.Script))
            {
                App.Options.Script = renewal.Script;
            }
            if (!string.IsNullOrWhiteSpace(renewal.ScriptParameters))
            {
                App.Options.ScriptParameters = renewal.ScriptParameters;
            }
            if (renewal.Warmup)
            {
                App.Options.Warmup = true;
            }
            renewal.Binding.Plugin.Renew(renewal.Binding);

            renewal.Date = DateTime.UtcNow.AddDays(App.Options.RenewalPeriodDays);
            App.Options.Settings.SaveRenewals(renewals);

            Log.Information("Renewal Scheduled {renewal}", renewal);
        }
    }
}
