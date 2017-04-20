using System.IO;
using System.Net;
using ACMESharp;
using ACMESharp.JOSE;
using LetsEncryptWinSimple.Core.Interfaces;
using Serilog;

namespace LetsEncryptWinSimple.Core.Services
{
    public class AcmeClientService : IAcmeClientService
    {
        protected IOptions Options;
        protected ICertificateService CertificateService;
        protected IConsoleService ConsoleService;
        public AcmeClientService(IOptions options, ICertificateService certificateService, 
            IConsoleService consoleService)
        {
            Options = options;
            CertificateService = certificateService;
            ConsoleService = consoleService;
        }

        public void ConfigureAcmeClient(AcmeClient client)
        {
            if (!string.IsNullOrWhiteSpace(Options.Proxy))
            {
                client.Proxy = new WebProxy(Options.Proxy);
                Log.Information("Proxying via {Proxy}", Options.Proxy);
            }

            client.Init();

            Log.Information("Getting AcmeServerDirectory");
            client.GetDirectory(true);

            if (Options.Renew)
            {
                CertificateService.CheckRenewalsAndWaitForEnterKey();
                return;
            }

            var registrationPath = Path.Combine(Options.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
            {
                LoadRegistrationFromFile(client, registrationPath);
            }
            else
            {
                var email = Options.SignerEmail;
                if (string.IsNullOrWhiteSpace(email))
                {
                    ConsoleService.WriteLine("Enter an email address (not public, used for renewal fail notices): ");
                    email = ConsoleService.ReadLine();
                }

                var contacts = GetContacts(email);
                var registration = CreateRegistration(client, contacts);

                if (!Options.AcceptTos && !Options.Renew)
                    if (!ConsoleService.PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                        return;

                UpdateRegistration(client);
                SaveRegistrationToFile(client, registrationPath);
            }

            Options.AcmeClient = client;
        }

        public void ConfigureSigner(RS256Signer signer)
        {
            signer.Init();
            var signerPath = Path.Combine(Options.ConfigPath, "Signer");
            if (File.Exists(signerPath))
                CertificateService.LoadSignerFromFile(signer, signerPath);
            Log.Information("Saving Signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        public AcmeRegistration CreateRegistration(AcmeClient acmeClient, string[] contacts)
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
            if (!string.IsNullOrEmpty(email))
            {
                Log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new[] { email };
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
