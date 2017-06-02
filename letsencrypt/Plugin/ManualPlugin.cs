using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using letsencrypt.Support;

namespace letsencrypt
{
    public class ManualPlugin : Plugin
    {
        private Dictionary<string, string> config;

        public string hostName;

        public string localPath;

        public override string Name => R.Manual;

        public override bool RequiresElevated => true;

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.M;

        public override bool Validate(Options options) => true;

        public override bool SelectOptions(Options options)
        {
            config = GetConfig(options);
            hostName = LetsEncrypt.GetString(config, "host_name");
            if (string.IsNullOrEmpty(hostName))
            {
                hostName = LetsEncrypt.PromptForText(options, R.Enterthesitehostname);
            }
            
            localPath = LetsEncrypt.GetString(config, "local_path");
            if (string.IsNullOrEmpty(localPath))
            {
                localPath = LetsEncrypt.PromptForText(options, R.EnterSitePath);
            }

            if (!string.IsNullOrEmpty(localPath))
            {
                localPath = Path.GetFullPath(localPath);
            }

            if (!Directory.Exists(localPath))
            {
                Log.Error(string.Format(R.Cannotfindthepath, localPath));
                return false;
            }

            return true;
        }

        public override void Install(Target target, Options options)
        {
            string pfxFilename = Auto(target, options);
            if (File.Exists(pfxFilename))
            {
                Log.Information(R.YoucanfindthecertificateatpfxFilename, pfxFilename);
            }else
            {
                Log.Error(R.CertificatecreationfailedFilenotfound, pfxFilename);
            }
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override List<Target> GetTargets(Options options)
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                Host = hostName,
                WebRootPath = localPath,
                PluginName = Name,
                AlternativeNames = AlternativeNames
            });
            return result;
        }

        public override void PrintMenu()
        {
            Console.WriteLine(R.ManualMenuOption);
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information(R.WritingchallengeanswertoanswerPath, answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }
    }
}