using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class HttpValidationFactory<T> : BaseValidationPluginFactory<T> where T : IValidationPlugin
    {
        public HttpValidationFactory(string name, string description) : base(name, description, AcmeProtocol.CHALLENGE_TYPE_HTTP) { }

        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            // Manual
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                do
                {
                    target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, inputService, WebrootHint());
                }
                while (!ValidateWebroot(target));
            }

            if (target.IIS == null)
            {
                target.IIS = optionsService.Options.ManualTargetIsIIS;
                if (target.IIS == false)
                {
                    target.IIS = inputService.PromptYesNo("Copy default web.config before validation?");
                }
            }
        }

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        public override void Default(Target target, IOptionsService optionsService)
        {
            if (target.IIS == null)
            {
                target.IIS = optionsService.Options.ManualTargetIsIIS;
            }
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetRequiredOption(nameof(optionsService.Options.WebRoot), optionsService.Options.WebRoot);
            }
            if (!ValidateWebroot(target))
            {
                throw new ArgumentException($"Invalid --webroot {target.WebRootPath}: {WebrootHint()[0]}");
            }
        }

        /// <summary>
        /// Check if the webroot makes sense
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateWebroot(Target target)
        {
            return true;
        }

        /// <summary>
        /// Hint to show about what the webroot should look like
        /// </summary>
        /// <returns></returns>
        public virtual string[] WebrootHint()
        {
            return new[] { "Enter a site path (the web root of the host for http authentication)" };
        }
    }

    abstract class HttpValidation : IValidationPlugin
    {
        public readonly string _templateWebConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");
        public virtual char PathSeparator => '\\';
        protected ILogService _log;
        protected IInputService _input;
        protected ScheduledRenewal _renewal;
        private ProxyService _proxyService;

        public HttpValidation(ILogService logService, IInputService inputService, ProxyService proxyService, ScheduledRenewal renewal)
        {
            _log = logService;
            _input = inputService;
            _proxyService = proxyService;
            _renewal = renewal;
        }

        public Action<AuthorizationState> PrepareChallenge(AuthorizeChallenge challenge, string identifier)
        {
            var httpChallenge = challenge.Challenge as HttpChallenge;
            Refresh(_renewal.Binding);
            CreateAuthorizationFile(_renewal.Binding, httpChallenge);
            BeforeAuthorize(_renewal.Binding, httpChallenge);

            _log.Information("Answer should now be browsable at {answerUri}", httpChallenge.FileUrl);
            if (_renewal.Test && _renewal.New)
            {
                if (_input.PromptYesNo("Try in default browser?"))
                {
                    Process.Start(httpChallenge.FileUrl);
                    _input.Wait();
                }
            }
            if (_renewal.Warmup)
            {
                _log.Information("Waiting for site to warmup...");
                WarmupSite(new Uri(httpChallenge.FileUrl));
            }

            return authzState => Cleanup(_renewal.Binding, httpChallenge);
        }

        private void WarmupSite(Uri uri)
        {
            var request = WebRequest.Create(uri);
            request.Proxy = _proxyService.GetWebProxy();
            try
            {
                using (var response = request.GetResponse()) { }
            }
            catch (Exception ex)
            {
                _log.Error("Error warming up site: {@ex}", ex);
            }
        }

        /// <summary>
        /// Handle clean-up steps
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        private void Cleanup(Target target, HttpChallenge challenge)
        {
            BeforeDelete(target, challenge);
            DeleteAuthorization(target, challenge);
        }

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        public virtual void CreateAuthorizationFile(Target target, HttpChallenge challenge)
        {
            WriteFile(CombinePath(target.WebRootPath, challenge.FilePath), challenge.FileContent);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeAuthorize(Target target, HttpChallenge challenge)
        {
            if (target.IIS == true)
            {
                _log.Debug("Writing web.config");
                var destination = CombinePath(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config"));
                var content = GetWebConfig(target);
                WriteFile(destination, content);
            }
        }

        /// <summary>
        /// Get the template for the web.config
        /// </summary>
        /// <returns></returns>
        internal virtual string GetWebConfig(Target target)
        {
            return File.ReadAllText(_templateWebConfig);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeDelete(Target target, HttpChallenge challenge)
        {
            if (target.IIS == true)
            {
                _log.Debug("Deleting web.config");
                DeleteFile(CombinePath(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config")));
            }
        }

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        public virtual void DeleteAuthorization(Target target, HttpChallenge challenge)
        {
            try
            {
                _log.Debug("Deleting answer");
                var path = CombinePath(target.WebRootPath, challenge.FilePath);
                DeleteFile(path);
                if (Properties.Settings.Default.CleanupFolders)
                {
                    path = path.Replace($"{PathSeparator}{challenge.Token}", "");
                    if (DeleteFolderIfEmpty(path)) {
                        var idx = path.LastIndexOf(PathSeparator);
                        if (idx >= 0)
                        {
                            path = path.Substring(0, path.LastIndexOf(PathSeparator));
                            DeleteFolderIfEmpty(path);
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
        public virtual string CombinePath(string root, string path)
        {
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
        public abstract void WriteFile(string path, string content);

        /// <summary>
        /// Delete file from specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract void DeleteFile(string path);

        /// <summary>
        /// Check if folder is empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract bool IsEmpty(string path);

        /// <summary>
        /// Delete folder if not empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract void DeleteFolder(string path);

        /// <summary>
        /// Refresh
        /// </summary>
        /// <param name="scheduled"></param>
        /// <returns></returns>
        public virtual void Refresh(Target scheduled) { }
    }
}
