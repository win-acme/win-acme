using System;
using System.Collections.Generic;
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

                            List<Target> targets = Target.GetTargetsSorted();
                            Target.WriteBindings(targets);

                            Console.WriteLine();
                            App.ConsoleService.PrintMenuForPlugins();
                            App.ConsoleService.PrintMenu(targets);
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
                    if (App.ConsoleService.PromptYesNo("Would you like to start again?"))
                        retry = true;
                }
            } while (retry);
        }
    }
}
