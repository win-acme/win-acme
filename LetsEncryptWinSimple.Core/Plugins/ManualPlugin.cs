using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using LetsEncryptWinSimple.Core.Configuration;
using LetsEncryptWinSimple.Core.Interfaces;
using Serilog;

namespace LetsEncryptWinSimple.Core.Plugins
{
    public class ManualPlugin : IPlugin
    {
        protected IOptions Options;
        protected IConsoleService ConsoleService;
        protected IPluginService PluginService;
        public ManualPlugin(IOptions options, IConsoleService consoleService,
            IPluginService pluginService)
        {
            Options = options;
            ConsoleService = consoleService;
            PluginService = pluginService;
        }

        public string Name => "Manual";

        public List<Target> GetTargets()
        {
            var result = new List<Target>();
            return result;
        }

        public List<Target> GetSites()
        {
            var result = new List<Target>();
            return result;
        }

        public void OnAuthorizeFail(Target target)
        {
        }

        public void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(Options.Script) &&
                !string.IsNullOrWhiteSpace(Options.ScriptParameters))
            {
                var parameters = string.Format(Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword, pfxFilename, store.Name, certificate.FriendlyName,
                    certificate.Thumbprint);
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

        public void Install(Target target)
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

        public void Renew(Target target)
        {
            Log.Warning(" WARNING: Unable to renew.");
        }

        public void PrintMenu()
        {
            if (!string.IsNullOrEmpty(Options.ManualHost))
            {
                var target = new Target
                {
                    Host = Options.ManualHost,
                    WebRootPath = Options.WebRoot,
                    PluginName = Name
                };
                PluginService.DefaultAction(target);
                Environment.Exit(0);
            }

            ConsoleService.WriteLine(" M: Generate a certificate manually.");
        }

        public void Auto(Target target)
        {
        }

        public void HandleMenuResponse(string response, List<Target> targets)
        {
            if (string.IsNullOrEmpty(response) == false && response != "m".ToLowerInvariant())
                return;

            ConsoleService.Write("Enter a host name: ");
            var hostName = ConsoleService.ReadLine();
            List<string> sanList = null;

            if (Options.San)
            {
                var alternativeNames = ConsoleService.GetSanNames();
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
                PluginService.DefaultAction(target);
            }
            else
            {
                Log.Error(
                    "You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
            }
        }

        public void BeforeAuthorize(Target target, string answerPath, string token)
        {
        }

        public void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            if(directory == null)
                throw new NullReferenceException("directory");

            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }

        public void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
        }
    }
}