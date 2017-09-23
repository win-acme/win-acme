using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class ScriptClient : Plugin
    {
        public const string PluginName = "Manual";
        public override string Name => PluginName;

        public static void RunScript(string script, string parameterTemplate, params string[] parameters)
        {
            if (!string.IsNullOrWhiteSpace(script) && !string.IsNullOrWhiteSpace(parameterTemplate))
            {
                var parametersFormat = string.Format(parameterTemplate, parameters);
                Program.Log.Information(true, "Running {script} with {parameters}", script, parametersFormat);
                Process.Start(script, parametersFormat);
            }
            else if (!string.IsNullOrWhiteSpace(script))
            {
                Program.Log.Information(true, "Running {script}", script);
                Process.Start(script);
            }
            else
            {
                Program.Log.Warning("Unable to configure server software.");
            }
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            RunScript(
                Program.Options.Script,
                Program.Options.ScriptParameters,
                target.Host,
                Properties.Settings.Default.PFXPassword,
                pfxFilename,
                store.Name,
                certificate.FriendlyName,
                certificate.Thumbprint);
        }

        public override void Install(Target target)
        {
            RunScript(
                Program.Options.Script,
                Program.Options.ScriptParameters,
                target.Host,
                Properties.Settings.Default.PFXPassword, 
                Program.Options.CentralSslStore);
        }

        public override void Renew(Target target) {
            Auto(target);
        }
    }
}