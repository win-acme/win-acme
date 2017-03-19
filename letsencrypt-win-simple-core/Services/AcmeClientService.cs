using System;
using System.IO;
using System.Net;
using ACMESharp;
using ACMESharp.JOSE;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Services
{
    public class AcmeClientService
    {
        public void ConfigureAcmeClient(AcmeClient client)
        {
            if (!string.IsNullOrWhiteSpace(App.Options.Proxy))
            {
                client.Proxy = new WebProxy(App.Options.Proxy);
                Log.Information("Proxying via {Proxy}", App.Options.Proxy);
            }
            client.Init();

            Log.Information("Getting AcmeServerDirectory");
            client.GetDirectory(true);

            if (App.Options.Renew)
            {
                App.CertificateService.CheckRenewalsAndWaitForEnterKey();
                return;
            }

            var registrationPath = Path.Combine(App.Options.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
                LoadRegistrationFromFile(client, registrationPath);
            else
            {
                string email = App.Options.SignerEmail;
                if (string.IsNullOrWhiteSpace(email))
                {
                    App.ConsoleService.WriteLine("Enter an email address (not public, used for renewal fail notices): ");
                    email = App.ConsoleService.ReadLine();
                }

                string[] contacts = GetContacts(email);

                AcmeRegistration registration = CreateRegistration(client, contacts);

                if (!App.Options.AcceptTos && !App.Options.Renew)
                {
                    if(!App.ConsoleService.PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                        return;
                }

                UpdateRegistration(client);
                SaveRegistrationToFile(client, registrationPath);
            }
            
            App.Options.AcmeClient = client;
        }

        public void ConfigureSigner(RS256Signer signer)
        {
            signer.Init();
            var signerPath = Path.Combine(App.Options.ConfigPath, "Signer");
            if (File.Exists(signerPath))
                App.CertificateService.LoadSignerFromFile(signer, signerPath);
            Log.Information("Saving Signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        internal AcmeRegistration CreateRegistration(AcmeClient acmeClient, string[] contacts)
        {
            Log.Information("Calling Register");
            var registration = acmeClient.Register(contacts);
            return registration;
        }

        public void LoadRegistrationFromFile(AcmeClient acmeClient, string registrationPath)
        {
            Log.Information("Loading Registration from {registrationPath}", registrationPath);
            using (var registrationStream = File.OpenRead(registrationPath))
                acmeClient.Registration = AcmeRegistration.Load(registrationStream);
        }

        private static string[] GetContacts(string email)
        {
            var contacts = new string[] { };
            if (!String.IsNullOrEmpty(email))
            {
                Log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }

            return contacts;
        }

        private static void SaveRegistrationToFile(AcmeClient acmeClient, string registrationPath)
        {
            Log.Information("Saving Registration");
            using (var registrationStream = File.OpenWrite(registrationPath))
                acmeClient.Registration.Save(registrationStream);
        }

        private static void UpdateRegistration(AcmeClient acmeClient)
        {
            Log.Information("Updating Registration");
            acmeClient.UpdateRegistration(true, true);
        }
    }
}
