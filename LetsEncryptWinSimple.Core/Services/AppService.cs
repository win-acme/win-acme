using System;
using System.Collections.Generic;
using System.Linq;
using ACMESharp;
using ACMESharp.JOSE;
using LetsEncryptWinSimple.Core.Configuration;
using LetsEncryptWinSimple.Core.Interfaces;
using Serilog;

namespace LetsEncryptWinSimple.Core.Services
{
    public class AppService : IAppService
    {
        protected IOptions Options;
        protected ICertificateService CertificateService;
        protected IConsoleService ConsoleService;
        protected IAcmeClientService AcmeClientService;
        public AppService(IOptions options, ICertificateService certificateService,
            IConsoleService consoleService, IAcmeClientService acmeClientService)
        {
            Options = options;
            CertificateService = certificateService;
            ConsoleService = consoleService;
            AcmeClientService = acmeClientService;
        }

        public void LaunchApp()
        {
            Log.Information("Let's Encrypt (Simple Windows ACME Client)");
            Log.Information("ACME Server: {BaseUri}", Options.BaseUri);
            if (Options.San)
                Log.Debug("San Option Enabled: Running per site and not per host");

            var retry = false;
            do
            {
                try
                {
                    using (var signer = new RS256Signer())
                    {
                        AcmeClientService.ConfigureSigner(signer);
                        
                        using (var acmeClient = new AcmeClient(new Uri(Options.BaseUri), new AcmeServerDirectory(), signer))
                        {
                            AcmeClientService.ConfigureAcmeClient(acmeClient);

                            // Check for a plugin specified in the options
                            // Only print the menus if there's no plugin specified
                            // Otherwise: you actually have no choice, the specified plugin will run
                            if (string.IsNullOrWhiteSpace(Options.Plugin))
                            {
                                var targets = GetTargetsSorted();
                                ConsoleService.WriteBindings(targets);
                                ConsoleService.PrintMenuForPlugins();

                                if (string.IsNullOrEmpty(Options.ManualHost))
                                {
                                    ConsoleService.WriteLine(" A: Get certificates for all hosts");
                                    ConsoleService.WriteLine(" Q: Quit");
                                    ConsoleService.Write("Choose from one of the menu options above: ");
                                    var command = ConsoleService.ReadCommandFromConsole();
                                    switch (command)
                                    {
                                        case "a":
                                            CertificateService.GetCertificatesForAllHosts(targets);
                                            break;
                                        case "q":
                                            return;
                                        default:
                                            CertificateService.ProcessDefaultCommand(targets, command);
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                                // Plugins that can run automatically should allow for an empty string as menu response to work
                                var plugin = Options.Plugins.Values.FirstOrDefault(x => x.Name == Options.Plugin);
                                if (plugin != null)
                                {
                                    var targets = new List<Target>();
                                    targets.AddRange(plugin.GetTargets());
                                    CertificateService.ProcessDefaultCommand(targets, string.Empty);
                                }
                            }
                        }
                    }

                    retry = false;
                    ConsoleService.PromptEnter();
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;

                    Log.Error("Error {@e}", e);
                    var acmeWebException = e as AcmeClient.AcmeWebException;
                    if (acmeWebException != null)
                        Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", acmeWebException.Message, acmeWebException.Response.ContentAsString);

                    ConsoleService.PromptEnter();
                }

                if (string.IsNullOrWhiteSpace(Options.Plugin) && Options.Renew)
                {
                    if (ConsoleService.PromptYesNo("Would you like to start again?"))
                        retry = true;
                }
            } while (retry);
        }

        public List<Target> GetTargetsSorted()
        {
            var targets = new List<Target>();
            if (!string.IsNullOrEmpty(Options.ManualHost))
                return targets;

            foreach (var plugin in Options.Plugins.Values)
                targets.AddRange(!Options.San ? plugin.GetTargets() : plugin.GetSites());

            return targets.OrderBy(p => p.ToString()).ToList();
        }
    }
}
