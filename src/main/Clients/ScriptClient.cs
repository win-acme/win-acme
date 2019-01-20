using PKISharp.WACS.Services;
using System;
using System.Diagnostics;
using System.Text;

namespace PKISharp.WACS.Clients
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
                var PSI = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(script))
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
                    var output = new StringBuilder();
                    process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data) && !string.Equals(e.Data, "null"))
                        {
                            output.AppendLine($"Error: {e.Data}"); _log.Error("Script error: {0}", e.Data);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit(TimeoutMinutes * 60 * 1000);

                    // Write consolidated output to event viewer
                    _log.Information(true, output.ToString());

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