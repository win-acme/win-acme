using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Plugins
{
    public class ManualPlugin : Plugin
    {
        protected IOptions Options;
        protected ICertificateService CertificateService;
        protected ILetsEncryptService LetsEncryptService;
        protected IConsoleService ConsoleService;
        public ManualPlugin(IOptions options, ICertificateService certificateService,
            ILetsEncryptService letsEncryptService, IConsoleService consoleService, 
            IPluginService pluginService) : base(pluginService)
        {
            Options = options;
            CertificateService = certificateService;
            LetsEncryptService = letsEncryptService;
            ConsoleService = consoleService;
        }

        public override string Name => "Manual";

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
            if (!string.IsNullOrWhiteSpace(Options.Script) &&
                !string.IsNullOrWhiteSpace(Options.ScriptParameters))
            {
                var parameters = string.Format(Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword,
                    pfxFilename, store.Name, certificate.FriendlyName, certificate.Thumbprint);
                Log.Information("Running {Script} with {parameters}", Options.Script, parameters);
                Process.Start(Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Options.Script))
            {
                Log.Information("Running {Script}", Options.Script);
                Process.Start(Options.Script);
            }
            else
            {
                Log.Warning(" WARNING: Unable to configure server software.");
            }
        }

        public override void Install(Target target)
        {
            // This method with just the Target paramater is currently only used by Centralized SSL
            if (!string.IsNullOrWhiteSpace(Options.Script) &&
                !string.IsNullOrWhiteSpace(Options.ScriptParameters))
            {
                var parameters = string.Format(Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword, Options.CentralSslStore);
                Log.Information("Running {Script} with {parameters}", Options.Script, parameters);
                Process.Start(Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Options.Script))
            {
                Log.Information("Running {Script}", Options.Script);
                Process.Start(Options.Script);
            }
            else
            {
                Log.Warning(" WARNING: Unable to configure server software.");
            }
        }

        public override void Renew(Target target)
        {
            Log.Warning(" WARNING: Unable to renew.");
        }

        public override void PrintMenu()
        {
            if (!String.IsNullOrEmpty(Options.ManualHost))
            {
                var target = new Target
                {
                    Host = Options.ManualHost,
                    WebRootPath = Options.WebRoot,
                    PluginName = Name
                };
                Auto(target);
                Environment.Exit(0);
            }

            ConsoleService.WriteLine(" M: Generate a certificate manually.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "m")
            {
                ConsoleService.Write("Enter a host name: ");
                var hostName = ConsoleService.ReadLine();
                string[] alternativeNames = null;
                List<string> sanList = null;

                if (Options.San)
                {
                    alternativeNames = ConsoleService.GetSanNames();
                    sanList = new List<string>(alternativeNames);
                }

                ConsoleService.Write("Enter a site path (the web root of the host for http authentication): ");
                var physicalPath = ConsoleService.ReadLine();

                if (sanList == null || sanList.Count <= 100)
                {
                    var target = new Target
                    {
                        Host = hostName,
                        WebRootPath = physicalPath,
                        PluginName = Name,
                        AlternativeNames = sanList
                    };
                    Auto(target);
                }
                else
                {
                    Log.Error(
                        "You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                }
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }
    }
}