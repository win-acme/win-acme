using ACMESharp.Authorizations;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for HTTP-01 validation plugins
    /// </summary>
    internal abstract class HttpValidation<TOptions, TPlugin> :
        Validation<Http01ChallengeValidationDetails>
        where TOptions : HttpValidationOptions<TPlugin>
        where TPlugin : IValidationPlugin
    {

        private bool _webConfigWritten = false;
        private bool _challengeWritten = false;

        protected TOptions _options;
        protected ILogService _log;
        protected IInputService _input;
        protected ISettingsService _settings;
        protected Renewal _renewal;
        protected RunLevel _runLevel;

        /// <summary>
        /// Path used for the current renewal, may not be same as _options.Path
        /// because of the "Split" function employed by IISSites target
        /// </summary>
        protected string? _path;

        /// <summary>
        /// Provides proxy settings for site warmup
        /// </summary>
        private readonly ProxyService _proxy;

        /// <summary>
        /// Where to find the template for the web.config that's copied to the webroot
        /// </summary>
        protected string TemplateWebConfig => Path.Combine(Path.GetDirectoryName(_settings.ExePath), "web_config.xml");

        /// <summary>
        /// Character to seperate folders, different for FTP 
        /// </summary>
        protected virtual char PathSeparator => '\\';

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="log"></param>
        /// <param name="input"></param>
        /// <param name="options"></param>
        /// <param name="proxy"></param>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <param name="runLevel"></param>
        /// <param name="identifier"></param>
        public HttpValidation(TOptions options, RunLevel runLevel, HttpValidationParameters pars)
        {
            _options = options;
            _runLevel = runLevel;
            _path = options.Path;
            _log = pars.LogService;
            _input = pars.InputService;
            _proxy = pars.ProxyService;
            _settings = pars.Settings;
            _renewal = pars.Renewal;
        }

        /// <summary>
        /// Handle http challenge
        /// </summary>
        public async override Task PrepareChallenge(ValidationContext context, Http01ChallengeValidationDetails challenge)
        {
            // Should always have a value, confirmed by RenewalExecutor
            // check only to satifiy the compiler
            if (context.TargetPart != null)
            {
                Refresh(context.TargetPart);
            }
            WriteAuthorizationFile(challenge);
            WriteWebConfig(challenge);
            _log.Information("Answer should now be browsable at {answerUri}", challenge.HttpResourceUrl);
            if (_runLevel.HasFlag(RunLevel.Test) && _renewal.New)
            {
                if (await _input.PromptYesNo("[--test] Try in default browser?", false))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = challenge.HttpResourceUrl,
                        UseShellExecute = true
                    });
                    await _input.Wait();
                }
            }

            string? foundValue = null;
            try
            {
                var value = await WarmupSite(challenge);
                if (Equals(value, challenge.HttpResourceValue))
                {
                    _log.Information("Preliminary validation looks good, but the ACME server will be more thorough");
                }
                else
                {
                    _log.Warning("Preliminary validation failed, the server answered '{value}' instead of '{expected}'. The ACME server might have a different perspective",
                        foundValue ?? "(null)",
                        challenge.HttpResourceValue);
                }
            }
            catch (HttpRequestException hrex)
            {
                _log.Warning("Preliminary validation failed because '{hrex}'", hrex.Message);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Preliminary validation failed");
            }
        }

        /// <summary>
        /// Warm up the target site, giving the application a little
        /// time to start up before the validation request comes in.
        /// Mostly relevant to classic FileSystem validation
        /// </summary>
        /// <param name="uri"></param>
        private async Task<string> WarmupSite(Http01ChallengeValidationDetails challenge)
        {
            using var client = _proxy.GetHttpClient(false);
            var response = await client.GetAsync(challenge.HttpResourceUrl);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        private void WriteAuthorizationFile(Http01ChallengeValidationDetails challenge)
        {
            if (_path == null)
            {
                throw new InvalidOperationException();
            }
            WriteFile(CombinePath(_path, challenge.HttpResourcePath), challenge.HttpResourceValue);
            _challengeWritten = true;
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        private void WriteWebConfig(Http01ChallengeValidationDetails challenge)
        {
            if (_path == null)
            {
                throw new InvalidOperationException();
            }
            if (_options.CopyWebConfig == true)
            {
                try
                {
                    _log.Debug("Writing web.config");
                    var partialPath = challenge.HttpResourcePath.Split('/').Last();
                    var destination = CombinePath(_path, challenge.HttpResourcePath.Replace(partialPath, "web.config"));
                    var content = GetWebConfig();
                    WriteFile(destination, content);
                    _webConfigWritten = true;
                } 
                catch (Exception ex)
                {
                    _log.Warning("Unable to write web.config: {ex}", ex.Message); ;
                }
            }
        }

        /// <summary>
        /// Get the template for the web.config
        /// </summary>
        /// <returns></returns>
        private string GetWebConfig() => File.ReadAllText(TemplateWebConfig);

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        private void DeleteWebConfig(Http01ChallengeValidationDetails challenge)
        {
            if (_path == null)
            {
                throw new InvalidOperationException();
            }
            if (_webConfigWritten)
            {
                _log.Debug("Deleting web.config");
                var partialPath = challenge.HttpResourcePath.Split('/').Last();
                var destination = CombinePath(_path, challenge.HttpResourcePath.Replace(partialPath, "web.config"));
                DeleteFile(destination);
            }
        }

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        private void DeleteAuthorization(Http01ChallengeValidationDetails challenge)
        {
            try
            {
                if (_path != null && _challengeWritten)
                {
                    _log.Debug("Deleting answer");
                    var path = CombinePath(_path, challenge.HttpResourcePath);
                    var partialPath = challenge.HttpResourcePath.Split('/').Last();
                    DeleteFile(path);
                    if (_settings.Validation.CleanupFolders)
                    {
                        path = path.Replace($"{PathSeparator}{partialPath}", "");
                        if (DeleteFolderIfEmpty(path))
                        {
                            var idx = path.LastIndexOf(PathSeparator);
                            if (idx >= 0)
                            {
                                path = path.Substring(0, path.LastIndexOf(PathSeparator));
                                DeleteFolderIfEmpty(path);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        /// <summary>
        /// Combine root path with relative path
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected virtual string CombinePath(string root, string path)
        {
            if (root == null) { root = string.Empty; }
            var expandedRoot = Environment.ExpandEnvironmentVariables(root);
            var trim = new[] { '/', '\\' };
            return $"{expandedRoot.TrimEnd(trim)}{PathSeparator}{path.TrimStart(trim).Replace('/', PathSeparator)}";
        }

        /// <summary>
        /// Delete folder if it's empty
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool DeleteFolderIfEmpty(string path)
        {
            if (IsEmpty(path))
            {
                DeleteFolder(path);
                return true;
            }
            else
            {
                _log.Debug("Additional files or folders exist in {folder}, not deleting.", path);
                return false;
            }
        }

        /// <summary>
        /// Write file with content to a specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        protected abstract void WriteFile(string path, string content);

        /// <summary>
        /// Delete file from specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract void DeleteFile(string path);

        /// <summary>
        /// Check if folder is empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract bool IsEmpty(string path);

        /// <summary>
        /// Delete folder if not empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract void DeleteFolder(string path);

        /// <summary>
        /// Refresh
        /// </summary>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        protected virtual void Refresh(TargetPart targetPart) { }

        /// <summary>
        /// Dispose
        /// </summary>
        public override Task CleanUp(ValidationContext context, Http01ChallengeValidationDetails challenge)
        {
            DeleteWebConfig(challenge);
            DeleteAuthorization(challenge);
            return Task.CompletedTask;
        }
    }
}
