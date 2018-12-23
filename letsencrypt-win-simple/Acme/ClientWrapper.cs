using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Acme
{
    internal class ClientWrapper
    {
        private AcmeProtocolClient _client;
        private ILogService _log;
        private IInputService _input;
        private SettingsService _settings;
        private IOptionsService _optionsService;
        private ProxyService _proxyService;

        public ClientWrapper(
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
            _proxyService = proxy;
            var init = ConfigureAcmeClient().Result;
        }

        private async Task<bool> ConfigureAcmeClient()
        {
            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = _proxyService.GetWebProxy(),
            };
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(_optionsService.Options.BaseUri)
            };
            IJwsTool signer = null;
            var registrationKey = LoadSignerFromFile();
            if (registrationKey != null)
            {
                signer = registrationKey.GenerateTool();
            }
            _client = new AcmeProtocolClient(httpClient, signer: signer)
            {
                BeforeHttpSend = (x, r) =>
                {
                    _log.Debug("Send {method} request to {uri}", r.Method, r.RequestUri);
                },
            };
            _client.Directory = await _client.GetDirectoryAsync();
            await _client.GetNonceAsync();
            _client.Account = await EnsureRegistration();
            if (_client.Account == null)
            {
                throw new Exception("AcmeClientWrapper was unable to establish an Acme account");
            }
            return true;
        }

        private async Task<AccountDetails> EnsureRegistration()
        {
            AccountDetails registration;
            var registrationPath = Path.Combine(_settings.ConfigPath, "Registration");
            if (File.Exists(registrationPath))
            {
                _log.Debug("Loading account information from {registrationPath}", registrationPath);
                registration = JsonConvert.DeserializeObject<AccountDetails>(File.ReadAllText(registrationPath));
            }
            else
            {
                var contacts = GetContacts();
                var (contentType, filename, content) = await _client.GetTermsOfServiceAsync();
                if (!_optionsService.Options.AcceptTos && !_optionsService.Options.Renew)
                {
                    var tosPath = Path.Combine(_settings.ConfigPath, filename);
                    File.WriteAllBytes(tosPath, content);
                    _input.Show($"TERMS OF SERVICE", tosPath);
                    if (!_input.PromptYesNo($"Do you agree?"))
                        return null;
                }
                registration = await _client.CreateAccountAsync(contacts, termsOfServiceAgreed: true);
                _log.Debug("Saving registration");
                var accountKey = new AccountKey
                {
                    KeyType = _client.Signer.JwsAlg,
                    KeyExport = _client.Signer.Export(),
                };
                SaveSignerToFile(accountKey);
                File.WriteAllText(registrationPath, JsonConvert.SerializeObject(registration));
            }
            return registration;
        }

        private string[] GetContacts()
        {
            var email = _optionsService.Options.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = _input.RequestString("Enter an email address for potential issues");
            }
            var contacts = new string[] { };
            if (!String.IsNullOrEmpty(email))
            {
                _log.Debug("Registration email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }
            return contacts;
        }

        private string SignerPath
        {
            get
            {
                return Path.Combine(_settings.ConfigPath, "Signer");
            }
        }

        private string RegistrationPath
        {
            get
            {
                return Path.Combine(_settings.ConfigPath, "Registration");
            }
        }

        private void SaveSignerToFile(AccountKey signer)
        {
            _log.Debug("Saving signer to {SignerPath}", SignerPath);
            File.WriteAllText(SignerPath, JsonConvert.SerializeObject(signer));
        }

        internal IChallengeValidationDetails GetChallengeDetails(Authorization auth, Challenge challenge)
        {
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, _client.Signer);
        }

        private AccountKey LoadSignerFromFile()
        {
            if (File.Exists(SignerPath))
            {
                try
                {
                    _log.Debug("Loading signer from {SignerPath}", SignerPath);
                    var signerString = File.ReadAllText(SignerPath);
                    return JsonConvert.DeserializeObject<AccountKey>(signerString);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to load signer");
                }
            }
            return null;
        }

        internal Challenge SubmitChallengeAnswer(Challenge challenge)
        {
            return _client.AnswerChallengeAsync(challenge.Url).Result;
        }

        internal Challenge DecodeChallenge(string url)
        {
            return _client.GetChallengeDetailsAsync(url).Result;
        }

        internal OrderDetails CreateOrder(IEnumerable<string> identifiers)
        {
            return _client.CreateOrderAsync(identifiers).Result;
        }

        internal Authorization GetAuthorizationDetails(string url)
        {
            return _client.GetAuthorizationDetailsAsync(url).Result;
        }

        internal OrderDetails SubmitCsr(OrderDetails details, byte[] csr)
        {
            return _client.FinalizeOrderAsync(details.Payload.Finalize, csr).Result;
        }

        internal byte[] GetCertificate(OrderDetails order)
        {
            return _client.GetOrderCertificateAsync(order).Result;
        }
    }
}
