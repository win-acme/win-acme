using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    public class IISSiteServerPlugin : Plugin
    {
        public override string Name => "IISSiteServer";
        //This plugin is designed to allow a user to select multiple sites for a single SAN certificate or to generate a single SAN certificate for the entire server.
        //This has seperate code from the main main Program.cs

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
            // TODO: make a system where they can execute a program/batch file to update whatever they need after install.
            Console.WriteLine(" WARNING: Unable to configure server software.");
        }
        public override void Install(Target target)
        {
            // TODO: make a system where they can execute a program/batch file to update whatever they need after install.
            // This method with just the Target paramater is currently only used by Centralized SSL
            Console.WriteLine(" WARNING: Unable to configure server software.");
        }

        public override void PrintMenu()
        {
            if (Program.Options.SAN)
            {
                Console.WriteLine(" S: Generate a single SAN certificate for multiple sites.");
            }
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "s")
            {
                Console.WriteLine("Running IISSiteServer Plugin");
                Log.Information("Running IISSiteServer Plugin");
                if (Program.Options.SAN)
                {
                    List<Target> SiteList = new List<Target>();

                    Console.WriteLine("Enter all Site IDs seperated by a comma");
                    Console.Write(" S: for all sites on the server ");
                    var SANInput = Console.ReadLine();
                    if (SANInput == "s")
                    {
                        SiteList.AddRange(targets);
                    }
                    else
                    {
                        string[] siteIDs = SANInput.Split(',');
                        foreach (var id in siteIDs)
                        {
                            SiteList.AddRange(targets.Where(t => t.SiteId.ToString() == id));
                        }
                    }
                    int hostCount = 0;
                    foreach (var site in SiteList)
                    {
                        hostCount = hostCount + site.AlternativeNames.Count();
                    }

                    if(hostCount > 100)
                    {
                        Console.WriteLine($" You have too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                        Log.Error("You have too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                        Environment.Exit(1);
                    }
                    Target TotalTarget = CreateTarget(SiteList);
                    ProcessTotaltarget(TotalTarget, SiteList);
                }
                else
                {
                    Console.WriteLine($"Please run the application with --san to generate a SAN certificate");
                    Log.Error("Please run the application with --san to generate a SAN certificate");
                }
            }
        }
        public override void Renew(Target target)
        {
            List<Target> RunSites = new List<Target>();
            List<Target> targets = new List<Target>();

            foreach (var plugin in Target.Plugins.Values)
            {
                if (plugin.Name == "IIS")
                {
                    targets.AddRange(plugin.GetSites());
                }
            }

            string[] siteIDs = target.Host.Split(',');
            foreach (var id in siteIDs)
            {
                RunSites.AddRange(targets.Where(t => t.SiteId.ToString() == id));
            }

            Target TotalTarget = CreateTarget(RunSites);
            ProcessTotaltarget(TotalTarget, RunSites);

        }

        private Target CreateTarget(List<Target> RunSites)
        {
            Target TotalTarget = new Target();
            TotalTarget.PluginName = Name;
            TotalTarget.SiteId = 0;
            TotalTarget.WebRootPath = "";

            foreach (var site in RunSites)
            {
                var auth = Program.Authorize(site);
                if (auth.Status != "valid")
                {
                    Console.WriteLine("All hosts under all sites need to pass authorization before you can continue");
                    Log.Error("All hosts under all sites need to pass authorization before you can continue.");
                    Environment.Exit(1);
                }
                else
                {
                    if (TotalTarget.Host == null)
                    {
                        TotalTarget.Host = site.SiteId.ToString();

                    }
                    else
                    {
                        TotalTarget.Host = String.Format("{0},{1}", TotalTarget.Host, site.SiteId);
                    }
                    if (TotalTarget.AlternativeNames == null)
                    {
                        TotalTarget.AlternativeNames = site.AlternativeNames;
                    }
                    else
                    {
                        TotalTarget.AlternativeNames.AddRange(site.AlternativeNames);
                    }
                }
            }
            return TotalTarget;
        }

        private void ProcessTotaltarget(Target TotalTarget, List<Target> RunSites)
        {
            var pfxFilename = Program.GetCertificate(TotalTarget);
            X509Store store;
            X509Certificate2 certificate;
            if (!Program.CentralSSL)
            {
                Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                Program.InstallCertificate(TotalTarget, pfxFilename, out store, out certificate);
                if (Program.Options.Test && !Program.Options.Renew)
                {
                    Console.WriteLine($"\nDo you want to add/update the certificate to your server software? (Y/N) ");
                    if (!Program.PromptYesNo())
                        return;
                }
                Log.Information("Installing Non-Central SSL Certificate in server software");
                foreach (var site in RunSites)
                {
                    site.Plugin.Install(site, pfxFilename, store, certificate);
                }
            }
            else if (!Program.Options.Renew)
            {
                //If it is using centralized SSL and renewing, it doesn't need to change the
                //binding since just the certificate needs to be updated at the central ssl path
                Log.Information("Updating new Central SSL Certificate");
                foreach (var site in RunSites)
                {
                    site.Plugin.Install(site);
                }
            }

            if (Program.Options.Test && !Program.Options.Renew)
            {
                Console.WriteLine($"\nDo you want to automatically renew this certificate in 60 days? This will add a task scheduler task. (Y/N) ");
                if (!Program.PromptYesNo())
                    return;
            }

            if (!Program.Options.Renew)
            {
                Log.Information("Adding renewal for {binding}", TotalTarget);
                Program.ScheduleRenewal(TotalTarget);
            }
        }
    }
}
