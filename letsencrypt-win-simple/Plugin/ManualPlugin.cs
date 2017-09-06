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

        public List<string> ParseSanList(string input)
        {
            var ret = new List<string>();
            if (!string.IsNullOrEmpty(input))
            {
                ret.AddRange(input.
                                ToLower().
                                Split(',').
                                Where(x => !string.IsNullOrWhiteSpace(x)).
                                Select(x => x.Trim()).
                                Distinct());
            }
            if (ret.Count > Settings.maxNames)
            {
                Program.Log.Error($"You entered too many hosts for a single certificate. Let's Encrypt currently has a maximum of {Settings.maxNames} alternative names per certificate.");
                return null;
            }
            if (ret.Count == 0)
            {
                Program.Log.Error("No host names provided.");
                return null;
            }
            return ret;
        }

        public Target InputTarget(string plugin, string[] pathQuestion)
        {
            List<string> sanList = ParseSanList(Program.Input.RequestString("Enter comma-separated list of host names, starting with the primary one"));
            if (sanList != null)
            {
                Program.Options.WebRoot = Program.Input.RequestString(pathQuestion);
                return new Target()
                {
                    Host = sanList.First(),
                    AlternativeNames = sanList,
                    WebRootPath = Program.Options.WebRoot,
                    PluginName = plugin,
                };
            }
            else
            {
                return null;
            }
        }

        public override void Run()
        {
            base.Run();
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