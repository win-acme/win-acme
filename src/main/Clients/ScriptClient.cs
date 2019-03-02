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

        public void RunScript(string script, string parameters)
        {
            if (!string.IsNullOrWhiteSpace(script))
            {
                var actualScript = Environment.ExpandEnvironmentVariables(script);
                var actualParameters = parameters;
                if (actualScript.EndsWith(".ps1"))
                {
                    actualScript = "powershell.exe";                  
                    actualParameters = $"-executionpolicy remotesigned &'{script}' {parameters.Replace("\"", "\"\"\"")}";
                }
                var PSI = new ProcessStartInfo(actualScript)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                if (!string.IsNullOrWhiteSpace(actualParameters))
                {
                    _log.Information(true, "Script {script} starting with parameters {parameters}", script, parameters);
                    PSI.Arguments = actualParameters;
                }
                else 
                {
                    _log.Information(true, "Script {script} starting", script);
                }
                try
                {
                    var process = new Process { StartInfo = PSI };
                    var output = new StringBuilder();
                    process.OutputDataReceived += (s, e) => {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data) && !string.Equals(e.Data, "null"))
                        {
                            output.AppendLine($"Error: {e.Data}");
                            _log.Error("Script error: {0}", e.Data);
                        }
                    };
                    var exited = false;
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        _log.Information(true, false, output.ToString());
                        exited = true;
                        if (process.ExitCode != 0)
                        {
                            _log.Error("Script finished with ExitCode {code}", process.ExitCode);
                        }
                    };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    var totalWait = 0;
                    var interval = 1000;
                    while (!exited && totalWait < TimeoutMinutes * 60 * 1000)
                    {
                        System.Threading.Thread.Sleep(interval);
                        totalWait += totalWait;
                    }
                    if (!exited)
                    {
                        _log.Error($"Script execution timed out after {TimeoutMinutes} minutes, trying to kill");
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Killing process {Id} failed", process.Id);
                        }
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