using ACMESharp;
using ACMESharp.JOSE;
using PKISharp.WACS.Services;
using System;
using System.IO;

namespace PKISharp.WACS.Clients
{
    internal class AcmeClientWrapper
    {
        private RS256Signer _signer;
        private AcmeServerDirectory _directory;
        private AcmeClient _client;
        private ILogService _log;
        private IInputService _input;
        private SettingsService _settings;
        private IOptionsService _optionsService;
        private ProxyService _proxyService;
        public AcmeClient Acme => _client;

        public AcmeClientWrapper(
            IInputService inputService, 
            IOptionsService optionsService, 
            ILogService log, 
            SettingsService settings,
            ProxyService proxy)
        {
            _log = log;
            _settings = settings;
            _optionsService = optionsService;
            _input = inputService;
            _directory = new AcmeServerDirectory();
            _signer = new RS256Signer();
            _signer.Init();
            _proxyService = proxy;
            _client = new AcmeClient(new Uri(optionsService.Options.BaseUri), _directory, _signer);
            ConfigureAcmeClient();
        }

        private void ConfigureAcmeClient()
        {
            _client.Proxy = _proxyService.GetWebProxy();

            var signerPath = Path.Combine(_settings.ConfigPath, "Signer");
            if (File.Exists(signerPath))
                LoadSignerFromFile(_client.Signer, signerPath);

            _client.Init();
            _client.BeforeGetResponseAction = (x) =>
            {
                _log.Debug("Send {method} request to {uri}", x.Method, x.RequestUri);
            };
            _log.Debug("Getting AcmeServerDirectory");
            _client.GetDirectory(true);

            var registrationPath = Path.Combine(_settings.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
                LoadRegistrationFromFile(registrationPath);
            else
            {
                var email = _optionsService.Options.EmailAddress;
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = _input.RequestString("Enter an email address (not public, used for renewal fail notices)");
                }

                var contacts = GetContacts(email);

                var registration = CreateRegistration(contacts);

                if (!_optionsService.Options.AcceptTos && !_optionsService.Options.Renew)
                {
                    if (!_input.PromptYesNo($"Do you agree to {registration.TosLinkUri}?"))
                        return;
                }

                UpdateRegistration();
                SaveRegistrationToFile(registrationPath);
                SaveSignerToFile(_client.Signer, signerPath);
            }
        }

        private AcmeRegistration CreateRegistration(string[] contacts)
        {
            _log.Debug("Calling register");
            var registration = _client.Register(contacts);
            return registration;
        }

        private void LoadRegistrationFromFile(string registrationPath)
        {
            _log.Debug("Loading registration from {registrationPath}", registrationPath);
            using (var registrationStream = File.OpenRead(registrationPath))
                _client.Registration = AcmeRegistration.Load(registrationStream);
        }

        private string[] GetContacts(string email)
        {
            var contacts = new string[] { };
            if (!String.IsNullOrEmpty(email))
            {
                _log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }

            return contacts;
        }

        private void SaveSignerToFile(ISigner signer, string signerPath)
        {
            _log.Debug("Saving signer");
            using (var signerStream = File.OpenWrite(signerPath))
                signer.Save(signerStream);
        }

        private void SaveRegistrationToFile(string registrationPath)
        {
            _log.Debug("Saving registration");
            using (var registrationStream = File.OpenWrite(registrationPath))
                _client.Registration.Save(registrationStream);
        }

        private void UpdateRegistration()
        {
            _log.Debug("Updating registration");
            _client.UpdateRegistration(true, true);
        }

        private void LoadSignerFromFile(ISigner signer, string signerPath)
        {
            _log.Debug("Loading signer from {signerPath}", signerPath);
            using (var signerStream = File.OpenRead(signerPath))
                signer.Load(signerStream);
        }
    }
}
