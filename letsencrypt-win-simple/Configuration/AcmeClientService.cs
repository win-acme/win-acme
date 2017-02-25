using System;
using System.Net;
using ACMESharp;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Configuration
{
    internal class AcmeClientService
    {
        internal AcmeClient ConfigureAcmeClient(AcmeClient client)
        {
            if (!string.IsNullOrWhiteSpace(App.Options.Proxy))
            {
                client.Proxy = new WebProxy(App.Options.Proxy);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Proxying via " + App.Options.Proxy);
                Console.ResetColor();
            }

            return client;
        }

        internal AcmeRegistration CreateRegistration(string[] contacts)
        {
            Log.Information("Calling Register");
            var registration = App.Options.AcmeClient.Register(contacts);
            return registration;
        }
    }
}
