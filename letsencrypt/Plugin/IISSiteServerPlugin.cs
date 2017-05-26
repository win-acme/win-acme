using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using letsencrypt.Support;

namespace letsencrypt
{
    public class IISSiteServerPlugin : IISPlugin
    {
        private List<Target> siteList;

        public override string Name => R.IISSiteServer;
        //This plugin is designed to allow a user to select multiple sites for a single San certificate or to generate a single San certificate for the entire server.

        public override bool RequiresElevated => true;
        
        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.S;

        public override bool SelectOptions(Options options)
        {
            if (options.San)
            {
                var targets = GetSites();
                siteList = new List<Target>();

                string hostName = LetsEncrypt.GetString(config, "host_name");
                if (string.IsNullOrEmpty(hostName))
                {
                    foreach (var target in targets)
                    {
                        Console.WriteLine($"{target.SiteId}: {target.Host}");
                    }
                    Console.WriteLine(R.EnterallsiteIDsseparatedbycommas);
                    Console.Write(R.IISSiteServerMenuOption2);
                    var sanInput = Console.ReadLine();
                    if (sanInput.ToLower() == "s")
                    {
                        siteList.AddRange(targets);
                    }
                    else
                    {
                        string[] siteIDs = sanInput.Split(',', ';', ' ');
                        siteList = targets.Where(t => siteIDs.Contains(t.SiteId.ToString())).ToList();
                    }
                }
                else
                {
                    string[] hostnames = hostName.Split(',', ';', ' ');
                    siteList = targets.Where(t => hostnames.Contains(t.Host)).ToList();
                }

                int hostCount = 0;
                foreach (var site in siteList)
                {
                    hostCount = hostCount + site.AlternativeNames.Count();
                }

                if (hostCount > 100)
                {
                    Log.Error(R.YouhavetoomanyhostsforaSancertificate);
                    return false;
                }
            }
            else
            {
                Log.Error(R.RuntheapplicationwithsantogenerateaSancertificate);
                return false;
            }
            return true;
        }

        public override List<Target> GetTargets(Options options)
        {
            var result = new List<Target>();
            result.Add(CreateTarget(siteList, options));
            return result;
        }

        public override void PrintMenu()
        {
            Console.WriteLine(R.IISSiteServerMenuOption);
        }

        public override string Auto(Target target, Options options)
        {
            ProcessTarget(target, siteList, options);
            return null;
        }

        public override void Renew(Target target, Options options)
        {
            List<Target> runSites = new List<Target>();
            List<Target> targets = GetSites();

            string[] siteIDs = target.Host.Split(',');
            foreach (var id in siteIDs)
            {
                runSites.AddRange(targets.Where(t => t.SiteId.ToString() == id));
            }
            
            ProcessTarget(target, runSites, options);
        }

        public override void Install(Target target, Options options)
        {
            ProcessTarget(target, siteList, options);
        }

        private Target CreateTarget(List<Target> sites, Options options)
        {
            Target totalTarget = new Target();
            totalTarget.PluginName = Name;
            totalTarget.SiteId = 0;
            totalTarget.WebRootPath = "";

            foreach (var site in sites)
            {
                var auth = Authorize(site, options);
                if (auth.Status != "valid")
                {
                    Log.Error(R.Allhostsunderallsitesneedtopassauthorizationbeforeyoucancontinue);
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

        private void ProcessTarget(Target target, List<Target> runSites, Options options)
        {
            X509Store store;
            X509Certificate2 certificate;
            if (!options.CentralSsl)
            {
                var pfxFilename = GetCertificate(target, client, options);
                Log.Information(R.InstallingSSLcertificateinthecertificatestore);
                InstallCertificate(target, pfxFilename, options, out store, out certificate);
                if (options.Test && !options.Renew)
                {
                    if (!LetsEncrypt.PromptYesNo(options, R.DoyouwanttoupdatethecertificateinIIS))
                    {
                        return;
                    }
                }
                Log.Information(R.InstallingSSLcertificateinIIS);
                foreach (var site in runSites)
                {
                    if (!options.KeepExisting)
                    {
                        UninstallCertificate(site.Host, certificate, options);
                    }
                }
            }
            else if (!options.Renew || !options.KeepExisting)
            {
                var pfxFilename = GetCertificate(target, client, options);
                //If it is using centralized SSL, renewing, and replacing existing it needs to replace the existing binding.
                Log.Information(R.InstallingcentralSSLcertificate);
                InstallCertificate(target, pfxFilename, options, out store, out certificate);
                foreach (var site in runSites)
                {
                    RunScript(site, pfxFilename, store, certificate, options);
                }
            }
        }
    }
}