using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public class ManualPlugin: Plugin
    {
        public override string Name => "Manual";

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();

            return result;
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            // TODO: make a system where they can execute a program/batch file to update whatever they need after install.
            Console.WriteLine(" WARNING: Unable to configure server software.");
        }

        public override void PrintMenu()
        {
            if (!String.IsNullOrEmpty(Program.Options.ManualHost))
            {
                var target = new Target() { Host = Program.Options.ManualHost, WebRootPath = Program.Options.WebRoot, PluginName = Name };
                Program.Auto(target);
                Environment.Exit(0);
            }

            Console.WriteLine(" M: Generate a certificate manually.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "m")
            {
                Console.Write("Enter a host name: ");
                var hostName = Console.ReadLine();

                // TODO: pull an existing host from the settings to default this value
                var physicalPath = String.Empty;
                var existingPath = new IISPlugin().GetPhysicalPath(hostName);
                if (existingPath != String.Empty)
                {
                    Console.Write("Use path: " + existingPath + " Y/N");
                    if (Console.ReadKey().ToString().ToLowerInvariant() == "y")
                        physicalPath = existingPath;
                    else
                    {
                        Console.Write("Enter a site path (the web root of the host for http authentication): ");
                        physicalPath = Console.ReadLine();
                    }
                }
                else
                {
                    Console.Write("Enter a site path (the web root of the host for http authentication): ");
                    physicalPath = Console.ReadLine();
                }

                // TODO: make a system where they can execute a program/batch file to update whatever they need after install.

                var target = new Target() { Host = hostName, WebRootPath = physicalPath, PluginName = Name };
                Program.Auto(target);
            }
        }
    }
}
