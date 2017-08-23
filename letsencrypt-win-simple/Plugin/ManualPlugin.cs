using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple
{
    public class ManualPlugin : Plugin
    {
        public override string Name => "Manual";

        public override List<Target> GetTargets()
        {
            if (!string.IsNullOrEmpty(Program.Options.ManualHost))
            {
                var target = new Target()
                {
                    Host = Program.Options.ManualHost,
                    WebRootPath = Program.Options.WebRoot,
                    PluginName = Name
                };
                return new List<Target>() { target };
            }
            return new List<Target>();
        }

        public override List<Target> GetSites()
        {
            if (!string.IsNullOrEmpty(Program.Options.ManualHost)) {
                var lsDomains = new List<string>();
                lsDomains = Program.Options.ManualHost.Split(',').ToList();
                lsDomains = lsDomains.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
                var target = new Target()
                {
                    Host = lsDomains[0],
                    WebRootPath = Program.Options.WebRoot,
                    PluginName = Name,
                    AlternativeNames = new List<string>(lsDomains)
                };
                return new List<Target>() { target };
            }
            return new List<Target>();
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(Program.Options.Script) &&
                !string.IsNullOrWhiteSpace(Program.Options.ScriptParameters))
            {
                var parameters = string.Format(Program.Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword,
                    pfxFilename, store.Name, certificate.FriendlyName, certificate.Thumbprint);
                Program.Log.Information(true, "Running {Script} with {parameters}", Program.Options.Script, parameters);
                Process.Start(Program.Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Program.Options.Script))
            {
                Program.Log.Information(true, "Running {Script}", Program.Options.Script);
                Process.Start(Program.Options.Script);
            }
            else
            {
                Program.Log.Warning("Unable to configure server software.");
            }
        }

        public override void Install(Target target)
        {
            // This method with just the Target paramater is currently only used by Centralized SSL
            if (!string.IsNullOrWhiteSpace(Program.Options.Script) &&
                !string.IsNullOrWhiteSpace(Program.Options.ScriptParameters))
            {
                var parameters = string.Format(Program.Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword, Program.Options.CentralSslStore);
                Program.Log.Information(true, "Running {Script} with {parameters}", Program.Options.Script, parameters);
                Process.Start(Program.Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Program.Options.Script))
            {
                Program.Log.Information(true, "Running {Script}", Program.Options.Script);
                Process.Start(Program.Options.Script);
            }
            else
            {
                Program.Log.Warning("Unable to configure server software.");
            }
        }

        public override void Renew(Target target) {
            Auto(target);
        }

        public override void PrintMenu() {
            Console.WriteLine(" M: Generate a certificate manually.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            Target target = null;
            if (response == "m")
            {
                var hostName = Input.RequestString("Enter a host name");
                string[] alternativeNames = null;
                List<string> sanList = null;

                if (Program.Options.San)
                {
                    Console.Write("Enter all Alternative Names seperated by a comma ");

                    // Copied from http://stackoverflow.com/a/16638000
                    int BufferSize = 16384;
                    Stream inputStream = Console.OpenStandardInput(BufferSize);
                    Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, BufferSize));

                    // Include host in the list of DNS names passed to LE
                    var sanInput = hostName + "," + Console.ReadLine();
                    alternativeNames = sanInput.Split(',');
                    sanList = new List<string>(alternativeNames);
                }

                Program.Options.WebRoot = Input.RequestString("Enter a site path (the web root of the host for http authentication)");

                var allNames = new List<string>();
                allNames.Add(hostName);
                allNames.AddRange(sanList ?? new List<string>());
                allNames = allNames.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();

                if (allNames.Count > Settings.maxNames)
                {
                    Program.Log.Error($"You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of {Settings.maxNames} alternative names per certificate.");
                    return;
                }
                if (allNames.Count == 0)
                {
                    Program.Log.Error("No host names provided.");
                    return;
                }
                target = new Target()
                {
                    Host = allNames.First(),
                    WebRootPath = Program.Options.WebRoot,
                    PluginName = Name,
                    AlternativeNames = allNames
                };
            }
            else
            {
                target = targets.Where(t => t.Plugin is ManualPlugin).FirstOrDefault();
            }

            if (target != null)
            {
                Auto(target);
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Program.Log.Debug("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }
    }
}