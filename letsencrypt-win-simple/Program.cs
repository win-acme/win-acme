using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using ACMESharp;
using ACMESharp.JOSE;
using LetsEncrypt.ACME.Simple.Core;
using Serilog;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple
{
    internal class Program
    {
        static bool IsElevated
            => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (IsNet45OrNewer() == false)
            {
                Log.Error("Error: You need to install .NET framework 4.5 on this machine in order to be able to run this app");
                return;
            }

            var app = new App();
            app.Initialize(args);
            if(App.Options == null)
                return;
            
            Log.Information("Let's Encrypt (Simple Windows ACME Client)");
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
                        App.AcmeClientService.ConfigureSigner(signer);
                        
                        using (var acmeClient = new AcmeClient(new Uri(App.Options.BaseUri), new AcmeServerDirectory(), signer))
                        {
                            App.AcmeClientService.ConfigureAcmeClient(acmeClient);
                            
                            // Check for a plugin specified in the options
                            // Only print the menus if there's no plugin specified
                            // Otherwise: you actually have no choice, the specified plugin will run
                            if (string.IsNullOrWhiteSpace(App.Options.Plugin))
                            {
                                var targets = Target.GetTargetsSorted();
                                Target.WriteBindings(targets);
                                App.ConsoleService.PrintMenuForPlugins();
                                App.ConsoleService.PrintMenu(targets);
                            }
                            else
                            {
                                // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                                // Plugins that can run automatically should allow for an empty string as menu response to work
                                var plugin = Target.Plugins.Values.FirstOrDefault(x => x.Name == App.Options.Plugin);
                                if (plugin != null)
                                {
                                    var targets = new List<Target>();
                                    targets.AddRange(plugin.GetTargets());
                                    Target.ProcessDefaultCommand(targets, string.Empty);
                                }
                            }
                        }
                    }

                    retry = false;
                    App.ConsoleService.PromptEnter();
                }
                catch (Exception e)
                {
                    Environment.ExitCode = e.HResult;

                    Log.Error("Error {@e}", e);
                    var acmeWebException = e as AcmeClient.AcmeWebException;
                    if (acmeWebException != null)
                        Log.Error("ACME Server Returned: {acmeWebExceptionMessage} - Response: {acmeWebExceptionResponse}", acmeWebException.Message, acmeWebException.Response.ContentAsString);

                    App.ConsoleService.PromptEnter();
                }

                if (string.IsNullOrWhiteSpace(App.Options.Plugin) && App.Options.Renew)
                {
                    if (App.ConsoleService.PromptYesNo("Would you like to start again?"))
                        retry = true;
                }
            } while (retry);
        }

        // From: http://stackoverflow.com/a/8543850/5018
        public static bool IsNet45OrNewer()
        {
            // Class "ReflectionContext" exists from .NET 4.5 onwards.
            return Type.GetType("System.Reflection.ReflectionContext", false) != null;
        }
    }
}
