using ACMESharp.Authorizations;
using PKISharp.WACS.Context;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
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
        private readonly List<string> _filesWritten = new List<string>();

        protected TOptions _options;
        protected ILogService _log;
        protected IInputService _input;
        protected ISettingsService _settings;
        protected Renewal _renewal;
        protected RunLevel _runLevel;

        /// <summary>
        /// Multiple http-01 validation challenges can be answered at the same time
        /// </summary>
        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        /// <summary>
        /// Path used for the current renewal, may not be same as _options.Path
        /// because of the "Split" function employed by IISSites target
        /// </summary>
        protected string? _path;

        /// <summary>
        /// Provides proxy settings for site warmup
        /// </summary>
        private readonly IProxyService _proxy;

        /// <summary>
        /// Where to find the template for the web.config that's copied to the webroot
        /// </summary>
        protected static string TemplateWebConfig => Path.Combine(VersionService.ResourcePath, "web_config.xml");

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
            await WriteAuthorizationFile(challenge);
            await WriteWebConfig(challenge);
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
        /// Default commit function, doesn't do anything because 
        /// default doesn't do parallel operation
        /// </summary>
        /// <returns></returns>
        public override Task Commit() => Task.CompletedTask;

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
        private async Task WriteAuthorizationFile(Http01ChallengeValidationDetails challenge)
        {
            if (_path == null)
            {
                throw new InvalidOperationException("No path specified for HttpValidation");
            }
            var path = CombinePath(_path, challenge.HttpResourcePath);
            await WriteFile(path, challenge.HttpResourceValue);
            if (!_filesWritten.Contains(path))
            {
                _filesWritten.Add(path);
            }
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        private async Task WriteWebConfig(Http01ChallengeValidationDetails challenge)
        {
            if (_path == null)
            {
                throw new InvalidOperationException("No path specified for HttpValidation");
            }
            if (_options.CopyWebConfig == true)
            {
                try
                {
                    var partialPath = challenge.HttpResourcePath.Split('/').Last();
                    var destination = CombinePath(_path, challenge.HttpResourcePath.Replace(partialPath, "web.config"));
                    if (!_filesWritten.Contains(destination))
                    {
                        var content = HttpValidation<TOptions, TPlugin>.GetWebConfig().Value;
                        if (content != null)
                        {
                            _log.Debug("Writing web.config");
                            await WriteFile(destination, content);
                            _filesWritten.Add(destination);
                        }

                    }
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
        private static Lazy<string?> GetWebConfig() => new Lazy<string?>(() => {
            try
            {
                return File.ReadAllText(HttpValidation<TOptions, TPlugin>.TemplateWebConfig);
            } 
            catch 
            {
                return null;
            }
        });

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
        private async Task<bool> DeleteFolderIfEmpty(string path)
        {
            if (await IsEmpty(path))
            {
                await DeleteFolder(path);
                return true;
            }
            else
            {
                _log.Debug("Not deleting {path} because it doesn't exist or it's not empty.", path);
                return false;
            }
        }

        /// <summary>
        /// Write file with content to a specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        protected abstract Task WriteFile(string path, string content);

        /// <summary>
        /// Delete file from specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract Task DeleteFile(string path);

        /// <summary>
        /// Check if folder is empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract Task<bool> IsEmpty(string path);

        /// <summary>
        /// Delete folder if not empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        protected abstract Task DeleteFolder(string path);

        /// <summary>
        /// Refresh
        /// </summary>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        protected virtual void Refresh(TargetPart targetPart) { }

        /// <summary>
        /// Dispose
        /// </summary>
        public override async Task CleanUp()
        {
            try
            {
                if (_path != null)
                {
                    var folders = new List<string>();
                    foreach (var file in _filesWritten)
                    {
                        _log.Debug("Deleting files");
                        await DeleteFile(file);
                        var folder = file.Substring(0, file.LastIndexOf(PathSeparator));
                        if (!folders.Contains(folder))
                        {
                            folders.Add(folder);
                        }
                    }
                    if (_settings.Validation.CleanupFolders)
                    {
                        _log.Debug("Deleting empty folders");
                        foreach (var folder in folders)
                        {
                            if (await DeleteFolderIfEmpty(folder))
                            {
                                var idx = folder.LastIndexOf(PathSeparator);
                                if (idx >= 0)
                                {
                                    var parent = folder.Substring(0, folder.LastIndexOf(PathSeparator));
                                    await DeleteFolderIfEmpty(parent);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error occured while deleting folder structure");
            }
        }
    }
}
