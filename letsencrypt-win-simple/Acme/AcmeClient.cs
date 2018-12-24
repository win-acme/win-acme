using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Acme
{
    internal class AcmeClient
    {
        private const string RegistrationFileName = "Registration_v2";
        private const string SignerFileName = "Signer_v2";

        private AcmeProtocolClient _client;
        private ILogService _log;
        private IInputService _input;
        private SettingsService _settings;
        private IOptionsService _optionsService;
        private ProxyService _proxyService;

        public AcmeClient(
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

        #region - Account and registration -

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
            var accountSigner = AccountSigner;
            if (accountSigner != null)
            {
                signer = accountSigner.JwsTool();
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
            _client.Account = await LoadAccount();
            if (_client.Account == null)
            {
                throw new Exception("AcmeClient was unable to find or create an Acme account");
            }
            return true;
        }

        private async Task<AccountDetails> LoadAccount()
        {
            AccountDetails account;
            if (File.Exists(AccountPath))
            {
                _log.Debug("Loading account information from {registrationPath}", AccountPath);
                account = JsonConvert.DeserializeObject<AccountDetails>(File.ReadAllText(AccountPath));
            }
            else
            {
                var contacts = GetContacts();
                var (contentType, filename, content) = await _client.GetTermsOfServiceAsync();
                if (!_optionsService.Options.AcceptTos && !_optionsService.Options.Renew)
                {
                    var tosPath = Path.Combine(_settings.ConfigPath, filename);
                    File.WriteAllBytes(tosPath, content);
                    _input.Show($"Terms of service are located at ", tosPath);
                    if (_input.PromptYesNo($"Open in default application?"))
                        Process.Start(tosPath);
                    if (!_input.PromptYesNo($"Do you agree with the terms?"))
                        return null;
                }
                account = await _client.CreateAccountAsync(contacts, termsOfServiceAgreed: true);
                _log.Debug("Saving registration");
                var accountKey = new AccountSigner
                {
                    KeyType = _client.Signer.JwsAlg,
                    KeyExport = _client.Signer.Export(),
                };
                AccountSigner = accountKey;
                File.WriteAllText(AccountPath, JsonConvert.SerializeObject(account));
            }
            return account;
        }

        private string[] GetContacts()
        {
            var email = _optionsService.Options.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = _input.RequestString("Enter an email address that can be used to send notifications about potential problems and abuse");
            }
            var contacts = new string[] { };
            if (!string.IsNullOrEmpty(email))
            {
                _log.Debug("Contact email: {email}", email);
                email = "mailto:" + email;
                contacts = new string[] { email };
            }
            return contacts;
        }

        private string SignerPath
        {
            get
            {
                return Path.Combine(_settings.ConfigPath, SignerFileName);
            }
        }

        private string AccountPath
        {
            get
            {
                return Path.Combine(_settings.ConfigPath, RegistrationFileName);
            }
        }

        private AccountSigner AccountSigner
        {
            get
            {
                if (File.Exists(SignerPath))
                {
                    try
                    {
                        _log.Debug("Loading signer from {SignerPath}", SignerPath);
                        var signerString = File.ReadAllText(SignerPath);
                        return JsonConvert.DeserializeObject<AccountSigner>(signerString);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to load signer");
                    }
                }
                return null;
            }
            set
            {
                _log.Debug("Saving signer to {SignerPath}", SignerPath);
                File.WriteAllText(SignerPath, JsonConvert.SerializeObject(value));
            }
        }

        #endregion

        internal IChallengeValidationDetails GetChallengeDetails(Authorization auth, Challenge challenge)
        {
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, _client.Signer);
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
