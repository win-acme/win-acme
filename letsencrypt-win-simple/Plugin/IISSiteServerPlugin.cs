using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple
{
    public class IISSiteServerPlugin : Plugin
    {
        public override string Name => "IISSiteServer";
        //This plugin is designed to allow a user to select multiple sites for a single San certificate or to generate a single San certificate for the entire server.
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
            Program.Log.Warning("Unable to configure server software.");
        }

        public override void Install(Target target)
        {
            // TODO: make a system where they can execute a program/batch file to update whatever they need after install.
            // This method with just the Target paramater is currently only used by Centralized SSL
            Program.Log.Warning("Unable to configure server software.");
        }

        public override void PrintMenu()
        {
            if (Program.Options.San)
            {
                Console.WriteLine(" S: Generate a single San certificate for multiple sites.");
            }
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            // Exclude DNS validation targets
            targets = targets.Where(t => t.PluginName == "IIS").ToList();

            if (response == "s")
            {
                Program.Log.Debug("Running IISSiteServer Plugin");
                if (Program.Options.San)
                {
                    List<Target> siteList = new List<Target>();
                    var sanInput = Program.Input.RequestString("Enter a comma separated list of site IDs, or 'S' to run for all sites");
                    if (sanInput.Trim().ToLower() == "s")
                    {
                        siteList.AddRange(targets);
                    }
                    else
                    {
                        string[] siteIDs = sanInput.Trim().Trim(',').Split(',');
                        foreach (var idString in siteIDs)
                        {
                            int id = -1;
                            if (int.TryParse(idString, out id))
                            {
                                var site = targets.Where(t => t.SiteId == id).FirstOrDefault();
                                if (site != null)
                                {
                                    siteList.Add(site);
                                }
                                else
                                {
                                    Program.Log.Warning($"SiteId '{idString}' not found");
                                }
                               
                            }
                            else
                            {
                                Program.Log.Warning($"Invalid SiteId '{idString}', should be a number");
                            }
                        }
                    }
                    int hostCount = siteList.Sum(x => x.AlternativeNames.Count());
                    if (hostCount > Settings.maxNames)
                    {
                        Program.Log.Error($"You have too many hosts for a San certificate. Let's Encrypt currently has a maximum of {Settings.maxNames} alternative names per certificate.");
                        Environment.Exit(1);
                    }
                    Target totalTarget = CreateTarget(siteList);
                    ProcessTotaltarget(totalTarget, siteList);
                }
                else
                {
                    Program.Log.Error("Please run the application with --san to generate a San certificate");
                }
            }
        }

        public override void Auto(Target target)
        {
            Console.WriteLine("Auto isn't supported for IISSiteServer Plugin");
        }

        public override void Renew(Target target)
        {
            List<Target> runSites = new List<Target>();
            List<Target> targets = new List<Target>();

            Plugin plugin;
            if (Target.Plugins.TryGetValue("IIS", out plugin))
            {
                targets.AddRange(plugin.GetSites());
            }

            string[] siteIDs = target.Host.Split(',');
            foreach (var id in siteIDs)
            {
                runSites.AddRange(targets.Where(t => t.SiteId.ToString() == id));
            }

            Target totalTarget = CreateTarget(runSites);
            ProcessTotaltarget(totalTarget, runSites);
        }

        private Target CreateTarget(List<Target> sites)
        {
            Target totalTarget = new Target();
            totalTarget.PluginName = Name;
            totalTarget.SiteId = 0;
            totalTarget.WebRootPath = "";

            foreach (var site in sites)
            {
                var auth = Program.Authorize(site);
                if (auth.Status != "valid")
                {
                    Program.Log.Error("All hosts under all sites need to pass authorization before you can continue.");
                    Environment.Exit(1);
                }
                else
                {
                    if (totalTarget.Host == null)
                    {
                        totalTarget.Host = site.SiteId.ToString();
                    }
                    else
                    {
                        totalTarget.Host = String.Format("{0},{1}", totalTarget.Host, site.SiteId);
                    }
                    if (totalTarget.AlternativeNames == null)
                    {
                        Target altNames = site.Copy();
                        //Had to copy the object otherwise the alternative names for the site were being updated from Totaltarget.
                        totalTarget.AlternativeNames = altNames.AlternativeNames;
                    }
                    else
                    {
                        Target altNames = site.Copy();
                        //Had to copy the object otherwise the alternative names for the site were being updated from Totaltarget.
                        totalTarget.AlternativeNames.AddRange(altNames.AlternativeNames);
                    }
                }
            }
            return totalTarget;
        }

        private static void ProcessTotaltarget(Target totalTarget, List<Target> runSites)
        {
            if (!Program.Options.CentralSsl)
            {

                var pfxFilename = "";
                try
                {
                    pfxFilename = Program.GetCertificate(totalTarget);
                }
                catch (Exception ex)
                {
                    Program.Log.Error(ex, "Unable to get certificate");
                    return;
                }
                
                X509Store store;
                X509Certificate2 certificate;
                Program.Log.Information("Installing non-Central SSL certificate in the certificate store");
                Program.InstallCertificate(totalTarget, pfxFilename, out store, out certificate);
                if (Program.Options.Test && !Program.Options.Renew)
                {
                    if (!Program.Input.PromptYesNo($"Do you want to add/update the certificate to your server software?"))
                        return;
                }
                Program.Log.Information("Installing non-Central SSL certificate in server software");
                foreach (var site in runSites)
                {
                    site.Plugin.Install(site, pfxFilename, store, certificate);
                }
                if (!Program.Options.KeepExisting)
                {
                    Program.UninstallCertificate(totalTarget.Host, out store, certificate);
                }
            }
            else if (!Program.Options.Renew || !Program.Options.KeepExisting)
            {
                var pfxFilename = Program.GetCertificate(totalTarget);
                //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                Program.Log.Information("Updating new Central SSL certificate");
                foreach (var site in runSites)
                {
                    site.Plugin.Install(site);
                }
            }

            if (Program.Options.Test && !Program.Options.Renew)
            {
                if (!Program.Input.PromptYesNo($"Do you want to automatically renew this certificate in {Program.RenewalPeriod} days? This will add a task scheduler task."))
                    return;
            }

            if (!Program.Options.Renew)
            {
                Program.Log.Information("Adding renewal for {binding}", totalTarget);
                Program.ScheduleRenewal(totalTarget);
            }
        }
    }
}