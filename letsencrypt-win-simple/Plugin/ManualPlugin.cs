using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    internal class ManualPlugin : Plugin
    {
        private string hostName;

        private string physicalPath;

        public override string Name => "Manual";

        public override bool RequiresElevated => true;

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.M;

        public override bool Validate() => true;

        public override bool SelectOptions(Options options)
        {
            Console.Write("Enter the site host name: ");
            hostName = Console.ReadLine();

            Console.Write("Enter a site path (the web root of the host for HTTP authentication): ");
            physicalPath = Console.ReadLine();
            if (!Directory.Exists(physicalPath))
            {
                Log.Error($"Cannot find the path: {physicalPath}");
                return false;
            }

            return true;
        }

        public override void Install(Target target, Options options)
        {
            Auto(target, options);
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                Host = hostName,
                WebRootPath = physicalPath,
                PluginName = Name,
                AlternativeNames = AlternativeNames
            });
            return result;
        }

        public override void PrintMenu()
        {
            Console.WriteLine(" M: Generate a certificate manually.");
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, fileContents);
        }
    }
}