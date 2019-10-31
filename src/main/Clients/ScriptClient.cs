using PKISharp.WACS.Services;
using System;
using System.Diagnostics;
using System.Text;
using Fclp.Internals.Extensions;

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

                    /*
                        if powershell script parameters contain whitespace, they will be wrapped into double quotes during arguments' parsing. 
                        See OptionArgumentParser.cs -> CollectArgumentsUntilNextKey
                        That means --scriptparameters "'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'"
                        will become "\"'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'\""
                        and after all substitutions are made, will be passed as "\"\"\"'{CertThumbprint}' 'IIS,SMTP,IMAP' 1 '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'\"\"\"" 
                        to the process argument. PowerShell will take that string as SINGLE parameter - and will display 
                        "Please enter parameter XXX" message for the rest of mandatory parameters in the script - and because window is invisible, 
                        script will just hang until process is killed.

                        therefore - if we detect that there are whitespaces in script argument AND it's wrapped in double quotes - then remove them
                    */
                    if (parameters.ContainsWhitespace() && parameters.IsWrappedInDoubleQuotes())
                    {
                        parameters = parameters.RemoveAnyWrappingDoubleQuotes();
                    }
                    actualParameters = $"-executionpolicy bypass &'{script}' {parameters.Replace("\"", "\"\"\"")}";
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
                    _log.Information(LogType.All, "Script {script} starting with parameters {parameters}", script, parameters);
                    PSI.Arguments = actualParameters;
                }
                else 
                {
                    _log.Information(LogType.All, "Script {script} starting", script);
                }
                try
                {
                    using (var process = new Process { StartInfo = PSI })
                    {
                        var output = new StringBuilder();
                        process.OutputDataReceived += (s, e) => {
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
                            _log.Information(LogType.All, output.ToString());
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
                        var totalWait = 0;
                        var interval = 2000;
                        while (!exited && totalWait < TimeoutMinutes * 60 * 1000)
                        {
                            System.Threading.Thread.Sleep(interval);
                            totalWait += interval;
                            _log.Verbose("Waiting for process to finish...");
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
