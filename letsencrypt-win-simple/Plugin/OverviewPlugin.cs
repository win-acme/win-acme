using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple
{
    public class OverviewPlugin : Plugin
    {
        private const string ClientName = "letsencrypt-win-simple";

        public override string Name => "OverviewPlugin";

        public override void PrintMenu()
        {
            Console.WriteLine(" O: Get overview of all bindings, protocols, renewals and certificates");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "o")
            {
                Console.WriteLine("Running Overview Plugin");
                Log.Information("Running Overview Plugin");

                GetIisSites();
                Console.WriteLine();
                Console.WriteLine("Press enter to continue.");
                Console.ReadKey();
                GetRenewals();
                Console.WriteLine();
                Console.WriteLine("Press enter to continue.");
                Console.ReadKey();
                GetCertificates();
                Console.WriteLine();
            }
        }

        public static void GetIisSites()
        {
            using (var iisManager = new ServerManager())
            {
                Console.WriteLine("IIS Sites:");
                foreach (var site in iisManager.Sites)
                {
                    Console.WriteLine("  " + site.Id + ": " + site.Name);
                    foreach (var binding in site.Bindings.OrderBy(o => o.Host))
                    {
                        if (!String.IsNullOrEmpty(binding.Host))
                        {
                            var line = "     - " + binding.Host + ((binding.Protocol == "https") ? " (https)" : "");
                            Console.WriteLine(line);
                        }
                    }
                }
            }
        }

        public static void GetCertificates()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Certificates:");
                string _certificateStore = "WebHosting";
                var store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                var certificates = store.Certificates;
                var i = 0;
                foreach (var certificate in certificates)
                {
                    i++;
                    Console.WriteLine("  " + certificate.FriendlyName);
                }
                Console.WriteLine();
                Console.WriteLine("  " + i + " certificates in total");
                store.Close();
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }
        }

        public static void GetRenewals()
        {
            try
            {
                var _settings = new Settings(ClientName, Program.Options.BaseUri);
                var renewals = _settings.LoadRenewals();
                Console.WriteLine();
                Console.WriteLine("Renewals:");
                var i = 0;
                foreach (var renewal in renewals.OrderBy(o => o.Binding.Host))
                {
                    i++;
                    var txt = ("  " + renewal.Binding.Host + (!String.IsNullOrEmpty(renewal.San) && renewal.San.ToLowerInvariant() == "true" ? " (SAN) " : " ")).PadRight(40);
                    Console.WriteLine(txt + "Renewal date: " + renewal.Date.ToShortDateString());
                    if (renewal.Binding.AlternativeNames != null && renewal.Binding.AlternativeNames.Any())
                    {
                        foreach (var item in renewal.Binding.AlternativeNames)
                        {
                            Console.WriteLine("    - " + item);
                        }
                    }
                }
                Console.WriteLine();
                Console.WriteLine("  " + i + " renewals in total");
            }
            catch (Exception ex)
            {
                Log.Error("Error encountered while loading renewals. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }
        }

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();

            return result;
        }

        public override List<Target> GetSites()
        {
            var result = new List<Target>();

            return result;
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            Console.WriteLine(" WARNING: Unable to configure server software.");
        }

        public override void Install(Target target)
        {
            Console.WriteLine(" WARNING: Unable to configure server software.");
        }

        public override void Auto(Target target)
        {
            Console.WriteLine("Auto isn't supported for Overview Plugin");
        }

        public override void Renew(Target target)
        {
            Console.WriteLine("Renew isn't supported for Overview Plugin");
        }
    }
}
