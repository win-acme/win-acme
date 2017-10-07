using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class HttpValidation : IValidationPlugin
    {
        public readonly string _templateWebConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_HTTP;
        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual char PathSeparator => '\\';

        public Action<AuthorizationState> PrepareChallenge(Target target, AuthorizeChallenge challenge, string identifier, Options options, InputService input)
        {
            var httpChallenge = challenge.Challenge as HttpChallenge;
     
            CreateAuthorizationFile(target, httpChallenge);
            BeforeAuthorize(target, httpChallenge);

            Program.Log.Information("Answer should now be browsable at {answerUri}", httpChallenge.FileUrl);
            if (options.Test && !options.Renew)
            {
                if (input.PromptYesNo("Try in default browser?"))
                {
                    Process.Start(httpChallenge.FileUrl);
                    input.Wait();
                }
            }
            if (options.Warmup)
            {
                Program.Log.Information("Waiting for site to warmup...");
                WarmupSite(new Uri(httpChallenge.FileUrl));
            }

            return authzState => Cleanup(target, httpChallenge);
        }

        private void WarmupSite(Uri uri)
        {
            var request = WebRequest.Create(uri);
            request.Proxy = Program.GetWebProxy();
            try
            {
                using (var response = request.GetResponse()) { }
            }
            catch (Exception ex)
            {
                Program.Log.Error("Error warming up site: {@ex}", ex);
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
                Program.Log.Debug("Writing web.config");
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
                Program.Log.Debug("Deleting web.config");
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
                Program.Log.Debug("Deleting answer");
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
                Program.Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
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

        public virtual bool CanValidate(Target target)
        {
            return true;
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
                Program.Log.Debug("Additional files or folders exist in {folder}, not deleting.", path);
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
        /// Prepare the plugin for validating the target (e.g. apply settings)
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual void Init(Options options, Target target) { }

        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        public virtual void Aquire(Options options, InputService input, Target target)
        {
            if (target.IIS == null)
            {
                target.IIS = options.ManualTargetIsIIS;
                if (target.IIS == null)
                {
                    target.IIS = input.PromptYesNo("Copy default web.config before validation?");
                }
            }
        }

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        public virtual void Default(Options options, Target target)
        {
            if (target.IIS == null)
            {
                target.IIS = options.ManualTargetIsIIS;
                if (target.IIS == null)
                {
                    target.IIS = false;
                }
            }
        }

        /// <summary>
        /// Create instance for specific target
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual IValidationPlugin CreateInstance(Target target)
        {
            return this;
        }
    }
}
