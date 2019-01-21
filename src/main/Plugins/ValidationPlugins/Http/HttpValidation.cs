using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for HTTP-01 validation plugins
    /// </summary>
    internal abstract class HttpValidation<TOptions, TPlugin> : 
        Validation<TOptions, Http01ChallengeValidationDetails>
        where TOptions : HttpValidationOptions<TPlugin>
        where TPlugin : IValidationPlugin
    {
        protected IInputService _input;
        protected Renewal _renewal;
        protected RunLevel _runLevel;

        /// <summary>
        /// Path used for the current renewal, may not be same as _options.Path
        /// because of the "Split" function employed by IISSites target
        /// </summary>
        protected string _path;

        /// <summary>
        /// Provides proxy settings for site warmup
        /// </summary>
        private ProxyService _proxy;

        /// <summary>
        /// Current TargetPart that we are working on. A TargetPart is mainly used by 
        /// the IISSites TargetPlugin to indicate that we are working with different
        /// IIS sites
        /// </summary>
        protected TargetPart _targetPart;

        /// <summary>
        /// Where to find the template for the web.config that's copied to the webroot
        /// </summary>
        protected readonly string _templateWebConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

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
        public HttpValidation(TOptions options, HttpValidationParameters pars) :
            base(pars.LogService, options, pars.Identifier)
        {
            _input = pars.InputService;
            _proxy = pars.ProxyService;
            _renewal = pars.Renewal;
            _targetPart = pars.TargetPart;
            _path = options.Path;
        }

        /// <summary>
        /// Handle http challenge
        /// </summary>
        public override void PrepareChallenge()
        {
            Refresh();
            CreateAuthorizationFile();
            BeforeAuthorize();
            _log.Information("Answer should now be browsable at {answerUri}", _challenge.HttpResourceUrl);
            if (_runLevel.HasFlag(RunLevel.Test) && _renewal.New)
            {
                if (_input.PromptYesNo("[--test] Try in default browser?"))
                {
                    Process.Start(_challenge.HttpResourceUrl);
                    _input.Wait();
                }
            }
            if (_options.Warmup == true)
            {
                _log.Information("Waiting for site to warmup...");
                WarmupSite();
            }
        }

        /// <summary>
        /// Warm up the target site, giving the application a little
        /// time to start up before the validation request comes in.
        /// Mostly relevant to classic FileSystem validation
        /// </summary>
        /// <param name="uri"></param>
        private void WarmupSite()
        {
            try
            {
                GetContent(new Uri(_challenge.HttpResourceUrl));
            }
            catch (Exception ex)
            {
                _log.Error("Error warming up site: {@ex}", ex);
            }
        }

        /// <summary>
        /// Read content from Uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected string GetContent(Uri uri) {
            var request = WebRequest.Create(uri);
            request.Proxy = _proxy.GetWebProxy();
            using (var response = request.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                using (var responseReader = new StreamReader(responseStream))
                {
                    return responseReader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        private void CreateAuthorizationFile()
        {
            WriteFile(CombinePath(_path, _challenge.HttpResourcePath), _challenge.HttpResourceValue);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        protected virtual void BeforeAuthorize()
        {
            if (_options.CopyWebConfig == true)
            {
                _log.Debug("Writing web.config");
                var partialPath = _challenge.HttpResourcePath.Split('/').Last();
                var destination = CombinePath(_path, _challenge.HttpResourcePath.Replace(partialPath, "web.config"));
                var content = GetWebConfig();
                WriteFile(destination, content);
            }
        }

        /// <summary>
        /// Get the template for the web.config
        /// </summary>
        /// <returns></returns>
        private string GetWebConfig()
        {
            return File.ReadAllText(_templateWebConfig);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        protected virtual void BeforeDelete()
        {
            if (_options.CopyWebConfig == true && _challenge != null)
            {
                _log.Debug("Deleting web.config");
                var partialPath = _challenge.HttpResourcePath.Split('/').Last();
                var destination = CombinePath(_path, _challenge.HttpResourcePath.Replace(partialPath, "web.config"));
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
        private void DeleteAuthorization()
        {
            try
            {
                if (_challenge != null)
                {
                    _log.Debug("Deleting answer");
                    var path = CombinePath(_path, _challenge.HttpResourcePath);
                    var partialPath = _challenge.HttpResourcePath.Split('/').Last();
                    DeleteFile(path);
                    if (Properties.Settings.Default.CleanupFolders)
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
        protected virtual void Refresh() { }

        /// <summary>
        /// Dispose
        /// </summary>
        public override void CleanUp()
        {
            BeforeDelete();
            DeleteAuthorization();
        }
    }
}
