using PKISharp.WACS.Services;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    public class ScriptClient
    {
        protected ILogService _log;
        protected ISettingsService _settings;

        public ScriptClient(ILogService logService, ISettingsService settings)
        {
            _log = logService;
            _settings = settings;
        }


        public async Task<bool> RunScript(string script, string parameters, string? censoredParameters = null)
        {
            if (!string.IsNullOrWhiteSpace(script))
            {
                var actualScript = Environment.ExpandEnvironmentVariables(script);
                var actualParameters = parameters;
                if (actualScript.EndsWith(".ps1"))
                {
                    actualScript = "powershell.exe";
                    actualParameters = $"-windowstyle hidden -noninteractive -executionpolicy bypass -command \"&{{&'{script.Replace("'", "''")}' {parameters.Replace("\"", "\"\"\"")}}}\"";
                }
                var PSI = new ProcessStartInfo(actualScript)
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                if (!string.IsNullOrWhiteSpace(actualParameters))
                {
                    _log.Information(LogType.All, "Script {script} starting with parameters {parameters}", script, censoredParameters ?? parameters);
                    PSI.Arguments = actualParameters;
                }
                else
                {
                    _log.Information(LogType.All, "Script {script} starting", script);
                }
                try
                {
                    using var process = new Process { StartInfo = PSI };
                    var output = new StringBuilder();
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                            _log.Verbose(e.Data);
                        }
                        else
                        {
                            _log.Verbose("Process output without data received");
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data) && !string.Equals(e.Data, "null"))
                        {
                            output.AppendLine($"Error: {e.Data}");
                            _log.Error("Script error: {0}", e.Data);
                        }
                        else
                        {
                            _log.Verbose("Process error without data received");
                        }
                    };
                    var exited = false;
                    process.EnableRaisingEvents = true;
                    process.Exited += (s, e) =>
                    {
                        _log.Information(LogType.Event | LogType.Disk | LogType.Notification, output.ToString());
                        exited = true;
                        if (process.ExitCode != 0)
                        {
                            _log.Error("Script finished with exit code {code}", process.ExitCode);
                        }
                        else
                        {
                            _log.Information("Script finished");
                        }
                    };
                    if (process.Start())
                    {
                        _log.Debug("Process launched: {actualScript} (ID: {Id})", actualScript, process.Id);
                    }
                    else
                    {
                        throw new Exception("Process.Start() returned false");
                    }

                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Close(); // Helps end the process
                    var totalWait = 0;
                    var interval = 2000;
                    while (!exited && totalWait < _settings.Script.Timeout * 1000)
                    {
                        await Task.Delay(interval);
                        totalWait += interval;
                        _log.Verbose("Waiting for process to finish...");
                    }
                    if (!exited)
                    {
                        _log.Error($"Script execution timed out after {_settings.Script.Timeout} seconds, trying to kill");
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Killing process {Id} failed", process.Id);
                        }
                        return false;
                    } 
                    else
                    {
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Script is unable to start");
                    return false;
                }
            }
            else
            {
                _log.Warning("No script configured.");
                return false;
            }
        }
    }
}