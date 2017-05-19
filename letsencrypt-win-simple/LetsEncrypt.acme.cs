using ACMESharp;
using ACMESharp.JOSE;
using Serilog;
using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    partial class LetsEncrypt
    {
        private static AcmeClient CreateAcmeClient()
        {
            AcmeClient client;
            var signer = new RS256Signer();
            signer.Init();

            var signerPath = Path.Combine(Options.ConfigPath, "Signer");
            if (File.Exists(signerPath))
            {
                Log.Information("Loading Signer from {signerPath}", signerPath);
                using (var signerStream = File.OpenRead(signerPath))
                {
                    signer.Load(signerStream);
                }
            }

            client = new AcmeClient(new Uri(Options.BaseUri), new AcmeServerDirectory(), signer);

            if (!string.IsNullOrWhiteSpace(Options.Proxy))
            {
                client.Proxy = new WebProxy(Options.Proxy);
                Log.Information("Proxying via " + Options.Proxy);
            }
            client.Init();

            Log.Information("Getting Acme Server Directory");
            client.GetDirectory(true);

            var registrationPath = Path.Combine(Options.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
            {
                LoadRegistrationFromFile(registrationPath, client);
            }
            else
            {
                string email = Options.SignerEmail;
                if (!Options.Silent && string.IsNullOrWhiteSpace(email))
                {
                    Console.Write("Enter an email address (not public, used for renewal fail notices): ");
                    email = Console.ReadLine().Trim();
                }

                string[] contacts = GetContacts(email);

                Log.Information("Creating registration");
                AcmeRegistration registration = client.Register(contacts);

                if (!Options.AcceptTos && !Options.Renew)
                {
                    if (!PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                    {
                        return null;
                    }
                }

                UpdateRegistration(client);
                SaveRegistrationToFile(registrationPath, client);
                SaveSignerToFile(signer, signerPath);

            }
            return client;
        }

        private static void LoadRegistrationFromFile(string registrationPath, AcmeClient client)
        {
            Log.Information("Loading Registration from {registrationPath}", registrationPath);
            using (var registrationStream = File.OpenRead(registrationPath))
            {
                client.Registration = AcmeRegistration.Load(registrationStream);
            }
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

        private static void SaveSignerToFile(RS256Signer signer, string signerPath)
        {
            Log.Information("Saving Signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        private static void SaveRegistrationToFile(string registrationPath, AcmeClient client)
        {
            Log.Information("Saving Registration");
            using (var registrationStream = File.OpenWrite(registrationPath))
            {
                client.Registration.Save(registrationStream);
            }
        }

        private static void UpdateRegistration(AcmeClient client)
        {
            Log.Information("Updating Registration");
            client.UpdateRegistration(true, true);
        }
    }
}
