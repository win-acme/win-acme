using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Microsoft.Web.Administration;

namespace LetsEncrypt.ACME.Simple
{
    public class SanByDomainPlugin : Plugin
    {
        public override string Name => "SanByDomain";
        private const int SanMax = 100;
        //This plugin is designed to allow a user to select one domain and all subdomains for a single San certificate across several sites.

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

        public override void PrintMenu()
        {
            Console.WriteLine(" D: Generate a single San certificate by Domain with subdomains.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "d")
            {
                Console.WriteLine("Running SanByDomain Plugin");
                Log.Information("Running SanByDomain Plugin");

                List<Domain> domains = new List<Domain>();

                using (var iisManager = new ServerManager())
                {
                    foreach (var site in iisManager.Sites)
                    {
                        foreach (var binding in site.Bindings)
                        {
                            if (!String.IsNullOrEmpty(binding.Host))
                            {
                                string domain = GetDomain(binding.Host);

                                //Hosts can appear more than once with different protocols. We only need it once
                                var dom = domains.FirstOrDefault(o => o.Host == binding.Host);
                                if (dom != null)
                                {
                                    if (!dom.HasHttpsBinding)
                                        dom.HasHttpsBinding = (binding.Protocol == "https");
                                }
                                else
                                {
                                    domains.Add(new Domain
                                    {
                                        DomainName = domain,
                                        Host = binding.Host.Trim(),
                                        IsDomain = (domain == binding.Host.ToLowerInvariant().Trim()),
                                        HasHttpsBinding = (binding.Protocol == "https"),
                                        WebRootPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                                        SiteId = site.Id
                                    });
                                }
                            }
                        }
                    }
                }

                var i = 0;
                var curDom = "";
                foreach (var item in domains.OrderBy(o => o.DomainName).ThenBy(o => o.IsDomain == false).ThenBy(o => o.Host))
                {
                    if (item.DomainName != curDom)
                    {
                        i++;
                        var dom = domains.FirstOrDefault(o => o.Host == item.Host);
                        dom.DomainId = i;
                        Console.WriteLine();
                    }

                    var line = (((item.DomainId > 0) ? "  " + item.DomainId + ": " : "  -- ") + item.Host + ((item.HasHttpsBinding) ? " (https)" : "")).PadRight(50) + " - " + item.DomainName;
                    Console.WriteLine(line);

                    curDom = item.DomainName;
                }

                Console.WriteLine();
                Console.WriteLine("Enter Id of domain to make SAN");
                var domainInput = Console.ReadLine();
                int domId = -1;
                try
                {
                    domId = Convert.ToInt32(domainInput);

                    var selecteddomain = domains.FirstOrDefault(o => o.DomainId == domId);
                    if (selecteddomain != null)
                    {
                        var sanDomains = domains.Where(o => o.DomainName == selecteddomain.DomainName).OrderBy(o => o.IsDomain == false).ThenBy(o => o.Host).ToList();

                        if (sanDomains.Count > SanMax)
                        {
                            Console.WriteLine($" You have too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of " + SanMax + " alternative names per certificate.");
                            Log.Error("You have too many hosts for a San certificate. Let's Encrypt currently has a maximum of " + SanMax + " alternative names per certificate.");
                        }
                        else
                        {
                            List<Target> siteList = new List<Target>();
                            List<string> altList = new List<string>();

                            foreach (var item in sanDomains.OrderBy(o => o.IsDomain == false).ThenBy(o => o.Host))
                            {
                                Target target = new Target();
                                target.Host = item.Host;
                                target.WebRootPath = item.WebRootPath;
                                target.SiteId = item.SiteId;
                                siteList.Add(target);
                            }

                            Target totalTarget = CreateTarget(siteList);
                            ProcessTotaltarget(totalTarget, siteList);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Id...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occured!");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
        }

        public override void Auto(Target target)
        {
            Console.WriteLine("Auto isn't supported for SanByDomain Plugin");
        }

        public override void Renew(Target target)
        {
            Console.WriteLine("Renew isn't supported for SanByDomain Plugin");
        }

        //Mostly copied from IISSiteServerPlugin
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
                    Console.WriteLine("All hosts under all sites need to pass authorization before you can continue");
                    Log.Error("All hosts under all sites need to pass authorization before you can continue.");
                    //If Environment.Exit(1) is executed here the error message will not be shown since the window closes!!!
                    //If not - the app continues and it will try to get certificates without auth...
                    //Environment.Exit(1);
                }
                else
                {
                    if (totalTarget.Host == null)
                    {
                        totalTarget.Host = site.Host;
                    }

                    if (totalTarget.AlternativeNames == null)
                    {
                        List<string> hostList = new List<string>();
                        hostList.Add(site.Host);
                        totalTarget.AlternativeNames = hostList;
                    }
                    else
                    {
                        totalTarget.AlternativeNames.Add(site.Host);
                    }
                }
            }
            return totalTarget;
        }

        //Copied from IISSiteServerPlugin
        private static void ProcessTotaltarget(Target totalTarget, List<Target> runSites)
        {
            if (!Program.CentralSsl)
            {
                var pfxFilename = Program.GetCertificate(totalTarget);
                X509Store store;
                X509Certificate2 certificate;
                Log.Information("Installing Non-Central SSL Certificate in the certificate store");
                Program.InstallCertificate(totalTarget, pfxFilename, out store, out certificate);
                if (Program.Options.Test && !Program.Options.Renew)
                {
                    Console.WriteLine($"\nDo you want to add/update the certificate to your server software? (Y/N) ");
                    if (!Program.PromptYesNo())
                        return;
                }
                Log.Information("Installing Non-Central SSL Certificate in server software");
                foreach (var site in runSites)
                {
                    site.Plugin.Install(site, pfxFilename, store, certificate);
                    if (!Program.Options.KeepExisting)
                    {
                        Program.UninstallCertificate(site.Host, out store, certificate);
                    }
                }
            }
            else if (!Program.Options.Renew || !Program.Options.KeepExisting)
            {
                var pfxFilename = Program.GetCertificate(totalTarget);
                //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                Log.Information("Updating new Central SSL Certificate");
                foreach (var site in runSites)
                {
                    site.Plugin.Install(site);
                }
            }

            if (Program.Options.Test && !Program.Options.Renew)
            {
                Console.WriteLine(
                    $"\nDo you want to automatically renew this certificate in {Program.RenewalPeriod} days? This will add a task scheduler task. (Y/N) ");
                if (!Program.PromptYesNo())
                    return;
            }

            if (!Program.Options.Renew)
            {
                Log.Information("Adding renewal for {binding}", totalTarget);
                Program.ScheduleRenewal(totalTarget);
            }
        }

        public string GetDomain(string input)
        {
            try
            {
                var nodes = input.Split('.');
                if (nodes.Length > 2)
                    return (nodes[nodes.Length - 2] + "." + nodes[nodes.Length - 1]).ToLowerInvariant().Trim();

                return input.ToLowerInvariant().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class Domain
    {
        public string DomainName { get; set; }
        public string Host { get; set; }
        public string WebRootPath { get; set; }
        public bool IsDomain { get; set; }
        public bool HasHttpsBinding { get; set; }
        public bool HasRenewal { get; set; }
        public int DomainId { get; set; }
        public long SiteId { get; set; }
    }
}
