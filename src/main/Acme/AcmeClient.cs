using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PKISharp.WACS.Acme
{
    internal class AcmeClient
    {
        public const int MaxNames = 100;
        private const string RegistrationFileName = "Registration_v2";
        private const string SignerFileName = "Signer_v2";

        private AcmeProtocolClient _client;
        private ILogService _log;
        private IInputService _input;
        private ISettingsService _settings;
        private IArgumentsService _arguments;
        private ProxyService _proxyService;

        public AcmeClient(
            IInputService inputService,
            IArgumentsService arguments,
            ILogService log,
            ISettingsService settings,
            ProxyService proxy)
        {
            _log = log;
            _settings = settings;
            _arguments = arguments;
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
                BaseAddress = new Uri(_arguments.MainArguments.GetBaseUri())
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
            _client.Account = await LoadAccount(signer);
            if (_client.Account == null)
            {
                throw new Exception("AcmeClient was unable to find or create an account");
            }
            return true;
        }

        internal AccountDetails Account {
            get
            {
                return _client.Account;
            }
        }

        private async Task<AccountDetails> LoadAccount(IJwsTool signer)
        {
            AccountDetails account = null;
            if (File.Exists(AccountPath))
            {
                if (signer != null)
                {
                    _log.Debug("Loading account information from {registrationPath}", AccountPath);
                    account = JsonConvert.DeserializeObject<AccountDetails>(File.ReadAllText(AccountPath));
                    _client.Account = account;
                    // Maybe we should update the account details 
                    // on every start of the program to figure out
                    // if it hasn't been suspended or cancelled?
                    // UpdateAccount();
                }
                else
                {
                    _log.Error("Account found but no valid Signer could be loaded");
                }
            }
            else
            {
                var contacts = GetContacts();
                var (contentType, filename, content) = await _client.GetTermsOfServiceAsync();
                if (!_arguments.MainArguments.AcceptTos)
                {
                    var tosPath = Path.Combine(_settings.ConfigPath, filename);
                    File.WriteAllBytes(tosPath, content);
                    _input.Show($"Terms of service", tosPath);
                    if (_input.PromptYesNo($"Open in default application?", false))
                        Process.Start(tosPath);
                    if (!_input.PromptYesNo($"Do you agree with the terms?", true))
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

        /// <summary>
        /// Get contact information
        /// </summary>
        /// <returns></returns>
        private string[] GetContacts()
        {
            var email = _arguments.MainArguments.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = _input.RequestString("Enter email(s) for notifications about problems and abuse (comma seperated)");
            }
            var newEmails = email.ParseCsv();
            if (newEmails == null)
            {
                return new string[] { };
            }
            newEmails = newEmails.Where(x => {
                try
                {
                    new MailAddress(x);
                    return true;
                }
                catch
                {
                    _log.Warning($"Invalid email: {x}");
                    return false;
                }
            }).ToList();
            if (newEmails.Count() == 0)
            {
                _log.Warning("No (valid) email addresses specified");
            }
            return newEmails.Select(x => $"mailto:{x}").ToArray();
        }

        /// <summary>
        /// File that contains information about the signer, which
        /// cryptographically signs the messages sent to the ACME 
        /// server so that the account can be authenticated
        /// </summary>
        private string SignerPath
        {
            get
            {
                return Path.Combine(_settings.ConfigPath, SignerFileName);
            }
        }

        /// <summary>
        /// File that contains information about the account
        /// </summary>
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
                        try
                        {
                            signerString = signerString.Unprotect();
                        }
                        catch
                        {
                            _log.Error("Unable to decrypt signer, likely because it was created on " +
                                "another machine and the setting <EncryptConfig> is set. You may delete " +
                                "the files {Account} and {Signer} to create a new registration.", 
                                RegistrationFileName, 
                                SignerFileName);
                            return null;
                        }
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
                File.WriteAllText(SignerPath, JsonConvert.SerializeObject(value).Protect());
            }
        }
        internal void ExportSigner()
        {
            try
            { 
                var signer = AccountSigner;

                AccountSigner=signer; //forces a re-save of the signer
                _log.Information("Signer re-saved");
            }
            catch
            {
                _log.Error("Cannot re-save signer as it is likely encrypted on a different machine");
            }
        }
        #endregion

        internal IChallengeValidationDetails DecodeChallengeValidation(Authorization auth, Challenge challenge)
        {
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, _client.Signer);
        }

        internal Challenge AnswerChallenge(Challenge challenge)
        {
            return Retry(() => _client.AnswerChallengeAsync(challenge.Url).Result);
        }

        internal OrderDetails CreateOrder(IEnumerable<string> identifiers)
        {
            return Retry(() => _client.CreateOrderAsync(identifiers).Result);
        }

        internal OrderDetails UpdateOrder(string orderUrl)
        {
            return Retry(() => _client.GetOrderDetailsAsync(orderUrl).Result);
        }

        internal Challenge GetChallengeDetails(string url)
        {
            return Retry(() => _client.GetChallengeDetailsAsync(url).Result);
        }

        internal Authorization GetAuthorizationDetails(string url)
        {
            return Retry(() => _client.GetAuthorizationDetailsAsync(url).Result);
        }

        internal OrderDetails SubmitCsr(OrderDetails details, byte[] csr)
        {
            return Retry(() => _client.FinalizeOrderAsync(details.Payload.Finalize, csr).Result);
        }

        internal void ChangeContacts()
        {
            var contacts = GetContacts();
            var account = Retry(() => _client.UpdateAccountAsync(contacts, _client.Account).Result);
            UpdateAccount();
        }

        internal void UpdateAccount()
        {
            var account = Retry(() => _client.CheckAccountAsync().Result);
            File.WriteAllText(AccountPath, JsonConvert.SerializeObject(account));
            _client.Account = account;
        }

        internal byte[] GetCertificate(OrderDetails order)
        {
            return Retry(() => _client.GetOrderCertificateAsync(order).Result);
        }

        internal void RevokeCertificate(byte[] crt)
        {
            Retry(() => _client.RevokeCertificateAsync(crt, RevokeReason.Unspecified));
        }

        /// <summary>
        /// According to the ACME standard, we SHOULD retry calls
        /// if there is an invalid nonce. TODO: check for the proper 
        /// exception feedback, now *any* failed request is retried
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        private T Retry<T>(Func<T> executor) {
            try
            {
                return executor();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is AcmeProtocolException)
                {
                    var apex = ex.InnerException as AcmeProtocolException;
                    if (apex.ProblemType == ProblemType.BadNonce)
                    {
                        _log.Warning("First chance error calling into ACME server, retrying with new nonce...");
                        _client.GetNonceAsync().Wait();
                        return executor();
                    }
                    throw ex.InnerException;
                }
                throw;
            }
        }

    }
}