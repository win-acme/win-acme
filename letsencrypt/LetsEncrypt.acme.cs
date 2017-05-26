using ACMESharp;
using ACMESharp.JOSE;
using letsencrypt.Support;
using Serilog;
using System;
using System.IO;
using System.Net;

namespace letsencrypt
{
    partial class LetsEncrypt
    {
        public static AcmeClient CreateAcmeClient(Options options)
        {
            AcmeClient client;
            var signer = new RS256Signer();
            signer.Init();

            var signerPath = Path.Combine(options.ConfigPath, "Signer");
            if (File.Exists(signerPath))
            {
                Log.Information(R.LoadingSignerfromsignerPath, signerPath);
                using (var signerStream = File.OpenRead(signerPath))
                {
                    signer.Load(signerStream);
                }
            }

            client = new AcmeClient(new Uri(options.BaseUri), new AcmeServerDirectory(), signer);

            if (!string.IsNullOrWhiteSpace(options.Proxy))
            {
                client.Proxy = new WebProxy(options.Proxy);
                Log.Information(string.Format(R.Proxyingvia, options.Proxy));
            }
            client.Init();

            Log.Information(R.GettingACMEserverdirectory);
            client.GetDirectory(true);

            var registrationPath = Path.Combine(options.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
            {
                LoadRegistrationFromFile(registrationPath, client);
            }
            else
            {
                string email = options.SignerEmail;
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = PromptForText(options, R.Enteranemailaddressnotpublicusedforrenewalfailnotices);
                }

                string[] contacts = GetContacts(email);

                Log.Information(R.Creatingregistration);
                AcmeRegistration registration = client.Register(contacts);

                if (!options.AcceptTos && !options.Renew)
                {
                    if (!PromptYesNo(options, string.Format(R.DoyouagreetoTosLinkUri, registration.TosLinkUri)))
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
            Log.Information(R.LoadingregistrationfromregistrationPath, registrationPath);
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
                email = "mailto:" + email;
                contacts = new string[] { email };
            }

            return contacts;
        }

        private static void SaveSignerToFile(RS256Signer signer, string signerPath)
        {
            Log.Information(R.Savingsigner);
            using (var signerStream = File.OpenWrite(signerPath))
            {
                signer.Save(signerStream);
            }
        }

        private static void SaveRegistrationToFile(string registrationPath, AcmeClient client)
        {
            Log.Information(R.Savingregistration);
            using (var registrationStream = File.OpenWrite(registrationPath))
            {
                client.Registration.Save(registrationStream);
            }
        }

        private static void UpdateRegistration(AcmeClient client)
        {
            Log.Information(R.Updatingregistration);
            client.UpdateRegistration(true, true);
        }
    }
}
