using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using ACMESharp;
using ACMESharp.JOSE;
using Serilog;
using LetsEncrypt.ACME.Simple.Configuration;

namespace LetsEncrypt.ACME.Simple
{
    internal class Program
    {
        static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var app = new App();
            app.Initialize(args);
            
            Console.WriteLine("Let's Encrypt (Simple Windows ACME Client)");
            Log.Information("ACME Server: {BaseUri}", App.Options.BaseUri);
            if (App.Options.San)
                Log.Debug("San Option Enabled: Running per site and not per host");
            
            bool retry = false;
            do
            {
                try
                {
                    using (var signer = new RS256Signer())
                    {
                        signer.Init();
                        
                        using (var acmeClient = new AcmeClient(new Uri(App.Options.BaseUri), new AcmeServerDirectory(), signer))
                        {
                            App.AcmeClientService.ConfigureAcmeClient(acmeClient, signer);

                            List<Target> targets = GetTargetsSorted();
                            WriteBindings(targets);

                            Console.WriteLine();
                            PrintMenuForPlugins();

                            if (string.IsNullOrEmpty(App.Options.ManualHost) && string.IsNullOrWhiteSpace(App.Options.Plugin))
                            {
                                Console.WriteLine(" A: Get certificates for all hosts");
                                Console.WriteLine(" Q: Quit");
                                Console.Write("Which host do you want to get a certificate for: ");
                                var command = ReadCommandFromConsole();
                                switch (command)
                                {
                                    case "a":
                                        App.CertificateService.GetCertificatesForAllHosts(targets);
                                        break;
                                    case "q":
                                        return;
                                    default:
                                        ProcessDefaultCommand(targets, command);
                                        break;
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
                            {
                                // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                                // Plugins that can run automatically should allow for an empty string as menu response to work
                                ProcessDefaultCommand(targets, string.Empty);
                            }
                        }
                    }

                    retry = false;
                    if (string.IsNullOrWhiteSpace(App.Options.Plugin))
					{
	                    Console.WriteLine("Press enter to continue.");
	                    Console.ReadLine();
					}
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;

                    Log.Error("Error {@e}", e);
                    var acmeWebException = e as AcmeClient.AcmeWebException;
                    if (acmeWebException != null)
						Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", acmeWebException.Message, acmeWebException.Response.ContentAsString);
                }

                if (string.IsNullOrWhiteSpace(App.Options.Plugin) && App.Options.Renew)
                {
                    Console.WriteLine("Would you like to start again? (y/n)");
                    if (ReadCommandFromConsole() == "y")
                        retry = true;
                }
            } while (retry);
        }

        private static void ProcessDefaultCommand(List<Target> targets, string command)
        {
            var targetId = 0;
            if (Int32.TryParse(command, out targetId))
            {
                App.CertificateService.GetCertificateForTargetId(targets, targetId);
                return;
            }

            HandleMenuResponseForPlugins(targets, command);
        }

        private static void HandleMenuResponseForPlugins(List<Target> targets, string command)
        {
            // Only run the plugin specified in the config
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                var plugin = Target.Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, App.Options.Plugin, StringComparison.InvariantCultureIgnoreCase));
                if (plugin != null)
                    plugin.HandleMenuResponse(command, targets);
                else
                {
                    Console.WriteLine($"Plugin '{App.Options.Plugin}' could not be found. Press enter to exit.");
                    Console.ReadLine();
                }
            }
            else
            {
                foreach (var plugin in Target.Plugins.Values)
                    plugin.HandleMenuResponse(command, targets);
            }
        }
        
        private static void WriteBindings(List<Target> targets)
        {
            if (targets.Count == 0 && string.IsNullOrEmpty(App.Options.ManualHost))
                WriteNoTargetsFound();
            else
            {
                int hostsPerPage = GetHostsPerPageFromSettings();

                if (targets.Count > hostsPerPage)
                    WriteBindingsFromTargetsPaged(targets, hostsPerPage, 1);
                else
                    WriteBindingsFromTargetsPaged(targets, targets.Count, 1);
            }
        }

        private static void PrintMenuForPlugins()
        {
            // Check for a plugin specified in the options
            // Only print the menus if there's no plugin specified
            // Otherwise: you actually have no choice, the specified plugin will run
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
                return;

            foreach (var plugin in Target.Plugins.Values)
            {
                if (string.IsNullOrEmpty(App.Options.ManualHost))
                {
                    plugin.PrintMenu();
                }
                else if (plugin.Name == "Manual")
                {
                    plugin.PrintMenu();
                }
            }
        }

        private static int GetHostsPerPageFromSettings()
        {
            int hostsPerPage = 50;
            try
            {
                hostsPerPage = Properties.Settings.Default.HostsPerPage;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting HostsPerPage setting, setting to default value. Error: {@ex}",
                    ex);
            }

            return hostsPerPage;
        }

        private static void WriteNoTargetsFound()
        {
            Log.Error("No targets found.");
        }
        
        private static int WriteBindingsFromTargetsPaged(List<Target> targets, int pageSize, int fromNumber)
        {
            do
            {
                int toNumber = fromNumber + pageSize;
                if (toNumber <= targets.Count)
                    fromNumber = WriteBindingsFomTargets(targets, toNumber, fromNumber);
                else
                    fromNumber = WriteBindingsFomTargets(targets, targets.Count + 1, fromNumber);

                if (fromNumber < targets.Count)
                {
                    WriteQuitCommandInformation();
                    string command = ReadCommandFromConsole();
                    switch (command)
                    {
                        case "q":
                            throw new Exception($"Requested to quit application");
                        default:
                            break;
                    }
                }
            } while (fromNumber < targets.Count);

            return fromNumber;
        }

        private static string ReadCommandFromConsole()
        {
            return Console.ReadLine().ToLowerInvariant();
        }

        private static void WriteQuitCommandInformation()
        {
            Console.WriteLine(" Q: Quit");
            Console.Write("Press enter to continue to next page ");
        }

        private static int WriteBindingsFomTargets(List<Target> targets, int toNumber, int fromNumber)
        {
            for (int i = fromNumber; i < toNumber; i++)
            {
                if (!App.Options.San)
                {
                    Console.WriteLine($" {i}: {targets[i - 1]}");
                }
                else
                {
                    Console.WriteLine($" {targets[i - 1].SiteId}: SAN - {targets[i - 1]}");
                }
                fromNumber++;
            }

            return fromNumber;
        }

        private static List<Target> GetTargetsSorted()
        {
            var targets = new List<Target>();
            if (!string.IsNullOrEmpty(App.Options.ManualHost))
                return targets;

            foreach (var plugin in Target.Plugins.Values)
            {
                targets.AddRange(!App.Options.San ? plugin.GetTargets() : plugin.GetSites());
            }

            return targets.OrderBy(p => p.ToString()).ToList();
        }
    }
}
