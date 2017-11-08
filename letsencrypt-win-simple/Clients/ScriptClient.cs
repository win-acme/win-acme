using Autofac;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Diagnostics;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class ScriptClient
    {
        private const int TimeoutMinutes = 5;
        protected ILogService _log;

        public ScriptClient(ILogService logService)
        {
            _log = logService;
        }

        public void RunScript(string script, string parameterTemplate, params string[] parameters)
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
                    _log.Information(true, "Script {script} starting with parameters {parameters}", script, parametersFormat);
                    PSI.Arguments = parametersFormat;
                }
                else 
                {
                    _log.Information(true, "Script {script} starting...", script);
                }
                try
                {
                    var process = new Process { StartInfo = PSI };
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) _log.Information("Script output: {0}", e.Data); };
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) _log.Error("Script error: {0}", e.Data); };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit(TimeoutMinutes * 60 * 1000);
                    if (!process.HasExited)
                    {
                        _log.Warning($"Script execution timed out after {TimeoutMinutes} minutes, will keep running in the background");
                    }
                    else if (process.ExitCode != 0)
                    {
                        _log.Warning("Script finished with ExitCode {code}", process.ExitCode);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Script is unable to start");
                }
            }
            else
            {
                _log.Warning("No script configured.");
            }
        }
    }
}