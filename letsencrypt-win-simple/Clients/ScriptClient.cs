using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class ScriptClient : Plugin
    {
        public const string PluginName = "Manual";
        private const int TimeoutMinutes = 5;

        public override string Name => PluginName;

        public static void RunScript(string script, string parameterTemplate, params string[] parameters)
        {
            if (!string.IsNullOrWhiteSpace(script))
            {
                ProcessStartInfo PSI = new ProcessStartInfo(script)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                if (!string.IsNullOrWhiteSpace(parameterTemplate))
                {
                    var parametersFormat = string.Format(parameterTemplate, parameters);
                    Program.Log.Information(true, "Script {script} starting with parameters {parameters}...", script, parametersFormat);
                    PSI.Arguments = parametersFormat;
                }
                else 
                {
                    Program.Log.Information(true, "Script {script} starting...", script);
                }
                try
                {
                    var process = new Process { StartInfo = PSI };
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) Program.Log.Information("Script output: {0}", e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) Program.Log.Error("Script error: {0}", e.Data); };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit(TimeoutMinutes * 60 * 1000);
                    if (!process.HasExited)
                    {
                        Program.Log.Warning($"Script execution timed out after {TimeoutMinutes} minutes, will keep running in the background");
                    }
                    else if (process.ExitCode != 0)
                    {
                        Program.Log.Warning("Script finished with ExitCode {code}", process.ExitCode);
                    }
                }
                catch (Exception ex)
                {
                    Program.Log.Error(ex, "Script is unable to start");
                }
            }
            else
            {
                Program.Log.Warning("No script configured.");
            }
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 newCertificate, X509Certificate2 oldCertificate)
        {
            RunScript(
                Program.Options.Script,
                Program.Options.ScriptParameters,
                target.Host,
                Properties.Settings.Default.PFXPassword,
                pfxFilename,
                store.Name,
                newCertificate.FriendlyName,
                newCertificate.Thumbprint);
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
    }
}