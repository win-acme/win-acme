using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Crypto.JOSE.Impl;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PKISharp.WACS.Acme
{
    internal class AcmeClient
    {
        private const string RegistrationFileName = "Registration_v2";
        private const string SignerFileName = "Signer_v2";

        private AcmeProtocolClient _client;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ISettingsService _settings;
        private readonly IArgumentsService _arguments;
        private readonly ProxyService _proxyService;
        private AccountSigner _accountSigner;
        private bool _initialized = false;

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
        }

        #region - Account and registration -

        internal async Task ConfigureAcmeClient()
        {
            var httpClient = _proxyService.GetHttpClient();
            httpClient.BaseAddress = new Uri(_settings.BaseUri);

            _log.Verbose("Loading ACME account signer...");
            IJwsTool signer = null;
            var accountSigner = AccountSigner;
            if (accountSigner != null)
            {
                signer = accountSigner.JwsTool();
            }

            _log.Verbose("Constructing ACME protocol client...");
            try
            {
                _client = new AcmeProtocolClient(httpClient, signer: signer);
            }
            catch (CryptographicException)
            {
                if (signer == null)
                {
                    // There has been a problem generate a signer for the 
                    // new account, possibly because some EC curve is not 
                    // on available on the system? So we give it another 
                    // shot with a less fancy RSA signer
                    _log.Verbose("First chance error generating new signer, retrying with RSA instead of ECC");
                    signer = new RSJwsTool
                    {
                        KeySize = _settings.RSAKeyBits
                    };
                    signer.Init();
                    _client = new AcmeProtocolClient(httpClient, signer: signer);
                }
                else
                {
                    throw;
                }
            }
            _client.BeforeHttpSend = (x, r) => _log.Debug("Send {method} request to {uri}", r.Method, r.RequestUri);
            _client.AfterHttpSend = (x, r) => _log.Verbose("Request completed with status {s}", r.StatusCode);
            _client.Directory = await _client.GetDirectoryAsync();
            await _client.GetNonceAsync();
            _client.Account = await LoadAccount(signer);
            if (_client.Account == null)
            {
                throw new Exception("AcmeClient was unable to find or create an account");
            }
        }

        internal async Task<AccountDetails> GetAccount() {
            await EnsureInitialized();
            return _client.Account;
        }

        internal async Task EnsureInitialized()
        {
            if (!_initialized)
            {
                await ConfigureAcmeClient();
                _initialized = true;
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
                    _log.Error("Account found but no valid signer could be loaded");
                }
            }
            else
            {
                var contacts = await GetContacts();
                var (_, filename, content) = await _client.GetTermsOfServiceAsync();
                if (!_arguments.MainArguments.AcceptTos)
                {
                    var tosPath = Path.Combine(_settings.ConfigPath, filename);
                    File.WriteAllBytes(tosPath, content);
                    _input.Show($"Terms of service", tosPath);
                    if (await _input.PromptYesNo($"Open in default application?", false))
                    {
                        Process.Start(tosPath);
                    }

                    if (!await _input.PromptYesNo($"Do you agree with the terms?", true))
                    {
                        return null;
                    }

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
        private async Task<string[]> GetContacts()
        {
            var email = _arguments.MainArguments.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = await _input.RequestString("Enter email(s) for notifications about problems and abuse (comma seperated)");
            }
            var newEmails = email.ParseCsv();
            if (newEmails == null)
            {
                return new string[] { };
            }
            newEmails = newEmails.Where(x =>
            {
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
        private string SignerPath => Path.Combine(_settings.ConfigPath, SignerFileName);

        /// <summary>
        /// File that contains information about the account
        /// </summary>
        private string AccountPath => Path.Combine(_settings.ConfigPath, RegistrationFileName);

        private AccountSigner AccountSigner
        {
            get
            {
                if (_accountSigner == null)
                {
                    if (File.Exists(SignerPath))
                    {
                        try
                        {
                            _log.Debug("Loading signer from {SignerPath}", SignerPath);
                            var signerString = new ProtectedString(File.ReadAllText(SignerPath), _log);
                            _accountSigner = JsonConvert.DeserializeObject<AccountSigner>(signerString.Value);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Unable to load signer");
                        }
                    }
                }
                return _accountSigner;
            }
            set
            {
                _log.Debug("Saving signer to {SignerPath}", SignerPath);
                var x = new ProtectedString(JsonConvert.SerializeObject(value));
                File.WriteAllText(SignerPath, x.DiskValue(_settings.EncryptConfig));
                _accountSigner = value;
            }
        }

        internal void EncryptSigner()
        {
            try
            {
                var signer = AccountSigner;
                AccountSigner = signer; //forces a re-save of the signer
                _log.Information("Signer re-saved");
            }
            catch
            {
                _log.Error("Cannot re-save signer as it is likely encrypted on a different machine");
            }
        }

        #endregion

        internal IChallengeValidationDetails DecodeChallengeValidation(Authorization auth, Challenge challenge) => AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, _client.Signer);

        internal async Task<Challenge> AnswerChallenge(Challenge challenge) => await Retry(() => _client.AnswerChallengeAsync(challenge.Url));

        internal async Task<OrderDetails> CreateOrder(IEnumerable<string> identifiers) => await Retry(() => _client.CreateOrderAsync(identifiers));

        internal async Task<OrderDetails> UpdateOrder(string orderUrl) => await Retry(() => _client.GetOrderDetailsAsync(orderUrl));

        internal async Task<Challenge> GetChallengeDetails(string url) => await Retry(() => _client.GetChallengeDetailsAsync(url));

        internal async Task<Authorization> GetAuthorizationDetails(string url) => await Retry(() => _client.GetAuthorizationDetailsAsync(url));

        internal async Task<OrderDetails> SubmitCsr(OrderDetails details, byte[] csr) => await Retry(() => _client.FinalizeOrderAsync(details.Payload.Finalize, csr));

        internal async Task ChangeContacts()
        {
            var contacts = await GetContacts();
            var account = await Retry(() => _client.UpdateAccountAsync(contacts, _client.Account));
            await UpdateAccount();
        }

        internal async Task UpdateAccount()
        {
            var account = await Retry(() => _client.CheckAccountAsync());
            File.WriteAllText(AccountPath, JsonConvert.SerializeObject(account));
            _client.Account = account;
        }

        internal async Task<byte[]> GetCertificate(OrderDetails order) => await Retry(() => _client.GetOrderCertificateAsync(order));

        internal async Task RevokeCertificate(byte[] crt) => await Retry(async () => {
            await _client.RevokeCertificateAsync(crt, RevokeReason.Unspecified);
            return Task.CompletedTask;
        });

        /// <summary>
        /// According to the ACME standard, we SHOULD retry calls
        /// if there is an invalid nonce. TODO: check for the proper 
        /// exception feedback, now *any* failed request is retried
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        private async Task<T> Retry<T>(Func<Task<T>> executor)
        {
            try
            {
                await EnsureInitialized();
                return await executor();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is AcmeProtocolException)
                {
                    var apex = ex.InnerException as AcmeProtocolException;
                    if (apex.ProblemType == ProblemType.BadNonce)
                    {
                        _log.Warning("First chance error calling into ACME server, retrying with new nonce...");
                        await _client.GetNonceAsync();
                        return await executor();
                    }
                    throw ex.InnerException;
                }
                throw;
            }
        }

    }
}