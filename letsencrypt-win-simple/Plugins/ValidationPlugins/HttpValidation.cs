using ACMESharp;
using ACMESharp.ACME;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class HttpValidation : IValidationPlugin
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_HTTP;

        public abstract string Name { get; }

        public Action<AuthorizationState> PrepareChallenge(Options options, Target target, AuthorizeChallenge challenge)
        {
            var webRootPath = Environment.ExpandEnvironmentVariables(target.WebRootPath);
            var httpChallenge = challenge.Challenge as HttpChallenge;
            var filePath = httpChallenge.FilePath.Replace('/', '\\');
            var answerPath = $"{webRootPath.TrimEnd('\\')}\\{filePath.TrimStart('\\')}";

            target.Plugin.CreateAuthorizationFile(answerPath, httpChallenge.FileContent);
            target.Plugin.BeforeAuthorize(target, answerPath, httpChallenge.Token);

            var answerUri = httpChallenge.FileUrl;

            Program.Log.Information("Answer should now be browsable at {answerUri}", answerUri);
            if (options.Test && !options.Renew)
            {
                if (Program.Input.PromptYesNo("Try in default browser?"))
                {
                    Process.Start(answerUri);
                    Program.Input.Wait();
                }
            }
            if (options.Warmup)
            {
                Program.Log.Information("Waiting for site to warmup...");
                WarmupSite(new Uri(answerUri));
            }

            return authzState =>
            {
                if (authzState.Status == "valid")
                {
                    target.Plugin.DeleteAuthorization(answerPath, httpChallenge.Token, webRootPath, filePath);
                }
            };
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
    }
}
