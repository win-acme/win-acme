using ACMESharp;
using ACMESharp.ACME;
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

        public Action<AuthorizationState> PrepareChallenge(Options options, Target target, AuthorizeChallenge challenge)
        {
            var webRootPath = Environment.ExpandEnvironmentVariables(target.WebRootPath);
            var httpChallenge = challenge.Challenge as HttpChallenge;
     
            CreateAuthorizationFile(options, target, httpChallenge);
            BeforeAuthorize(options, target, httpChallenge);

            Program.Log.Information("Answer should now be browsable at {answerUri}", httpChallenge.FileUrl);
            if (options.Test && !options.Renew)
            {
                if (Program.Input.PromptYesNo("Try in default browser?"))
                {
                    Process.Start(httpChallenge.FileUrl);
                    Program.Input.Wait();
                }
            }
            if (options.Warmup)
            {
                Program.Log.Information("Waiting for site to warmup...");
                WarmupSite(new Uri(httpChallenge.FileUrl));
            }

            return authzState => DeleteAuthorization(options, target, httpChallenge);
        }

        private void WarmupSite(Uri uri)
        {
            var request = WebRequest.Create(uri);
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Proxy))
            {
                request.Proxy = new WebProxy(Properties.Settings.Default.Proxy);
            }
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
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        public virtual void CreateAuthorizationFile(Options options, Target target, HttpChallenge challenge)
        {
            WriteFile(target.WebRootPath, challenge.FilePath, challenge.FileContent);
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeAuthorize(Options options, Target target, HttpChallenge challenge) {}

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeDelete(Options options, Target target, HttpChallenge challenge) {}

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        public virtual void DeleteAuthorization(Options options, Target target, HttpChallenge challenge)
        {
        }

        /// <summary>
        /// Write file with content to a specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public abstract void WriteFile(string root, string path, string content);

        /// <summary>
        /// Delete file from specific location
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract void DeleteFile(string root, string path);

        /// <summary>
        /// Check if folder is empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract void IsEmpty(string root, string path);

        /// <summary>
        /// Delete folder if not empty
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public abstract void DeleteFolder(string root, string path);

    }
}
