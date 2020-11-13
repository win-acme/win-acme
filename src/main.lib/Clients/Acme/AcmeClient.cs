using ACMESharp;
using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Crypto.JOSE.Impl;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Newtonsoft.Json;
using PKISharp.WACS.Configuration;
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
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.Acme
{
    /// <summary>
    /// Main class that talks to the ACME server
    /// </summary>
    internal class AcmeClient : IDisposable
    {
        private const string RegistrationFileName = "Registration_v2";
        private const string SignerFileName = "Signer_v2";

        /// <summary>
        /// https://tools.ietf.org/html/rfc8555#section-7.1.6
        /// </summary>
        public const string OrderPending = "pending"; // new order
        public const string OrderReady = "ready"; // all authorizations done
        public const string OrderProcessing = "processing"; // busy issuing
        public const string OrderInvalid = "invalid"; // validation/order error
        public const string OrderValid = "valid"; // certificate issued

        public const string AuthorizationValid = "valid";
        public const string AuthorizationInvalid = "invalid";
        public const string AuthorizationPending = "pending";
        public const string AuthorizationProcessing = "processing";

        public const string ChallengeValid = "valid";

        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ISettingsService _settings;
        private readonly IArgumentsService _arguments;
        private readonly ProxyService _proxyService;
        private readonly AccountArguments _accountArguments;
        private readonly ZeroSsl _zeroSsl;

        private AcmeProtocolClient? _client;
        private AccountSigner? _accountSigner;
        private bool _initialized = false;

        public AcmeClient(
            IInputService inputService,
            IArgumentsService arguments,
            ILogService log,
            ISettingsService settings,
            ProxyService proxy,
            ZeroSsl zeroSsl)
        {
            _log = log;
            _settings = settings;
            _arguments = arguments;
            _input = inputService;
            _proxyService = proxy;
            _accountArguments = _arguments.GetArguments<AccountArguments>() ?? new AccountArguments();
            _zeroSsl = zeroSsl;
        }

        #region - Account and registration -

        internal async Task ConfigureAcmeClient()
        {
            _log.Verbose("Loading ACME account signer...");
            var accountSigner = AccountSigner;
            IJwsTool? signer = null;
            if (accountSigner != null)
            {
                signer = accountSigner.JwsTool();
            }
            var httpClient = _proxyService.GetHttpClient();
            httpClient.BaseAddress = _settings.BaseUri;
            var client = PrepareClient(httpClient, signer);
            try
            {
                client.Directory = await client.GetDirectoryAsync();
            }
            catch (Exception)
            {
                // Perhaps the BaseUri *is* the directory, such
                // as implemented by Digicert (#1434)
                client.Directory.Directory = "";
                client.Directory = await client.GetDirectoryAsync();
            }
            await client.GetNonceAsync();
            client.Account = await LoadAccount(client, signer);
            if (client.Account == null)
            {
                throw new Exception("AcmeClient was unable to find or create an account");
            }
            _client = client;
            _log.Verbose("ACME client initialized");
        }

        internal AcmeProtocolClient PrepareClient(HttpClient httpClient, IJwsTool? signer)
        {
            _log.Verbose("Constructing ACME protocol client...");
            AcmeProtocolClient? client;
            try
            {
                client = new AcmeProtocolClient(
                    httpClient,
                    signer: signer,
                    usePostAsGet: _settings.Acme.PostAsGet);
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
                        KeySize = _settings.Security.RSAKeyBits
                    };
                    signer.Init();
                    client = new AcmeProtocolClient(
                        httpClient,
                        signer: signer,
                        usePostAsGet: _settings.Acme.PostAsGet);
                }
                else
                {
                    throw;
                }
            }
            return client;
        }

        internal async Task<AccountDetails?> GetAccount() => (await GetClient()).Account;

        internal async Task<AcmeProtocolClient> GetClient()
        {
            if (!_initialized)
            {
                await ConfigureAcmeClient();
                _initialized = true;
            }
            if (_client == null)
            {
                throw new InvalidOperationException("Failed to initialize AcmeProtocolClient");
            }
            return _client;
        }

        private async Task<AccountDetails?> LoadAccount(AcmeProtocolClient client, IJwsTool? signer)
        {
            _log.Verbose("Loading ACME account");
            AccountDetails? account = null;
            if (File.Exists(AccountPath))
            {
                if (signer != null)
                {
                    _log.Debug("Loading account information from {registrationPath}", AccountPath);
                    account = JsonConvert.DeserializeObject<AccountDetails>(File.ReadAllText(AccountPath));
                    client.Account = account;
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
                _log.Verbose("No account found at {path}, creating new one", AccountPath);
                try 
                {
                    var (_, filename, content) = await client.GetTermsOfServiceAsync();
                    _log.Verbose("Terms of service downloaded");
                    if (!string.IsNullOrEmpty(filename))
                    {
                        if (!await AcceptTos(filename, content))
                        {
                            return null;
                        }
                    }
                } 
                catch (Exception ex)
                {
                    _log.Error(ex, "Error getting terms of service");
                }
                var contacts = default(string[]);
                var externalAccount = default(ExternalAccountBinding);

                var kid = _accountArguments.EabKeyIdentifier;
                var key = _accountArguments.EabKey;
                var alg = _accountArguments.EabAlgorithm ?? "HS256";
                var eabFlow = client.Directory?.Meta?.ExternalAccountRequired == "true";
                var zeroSslFlow = _settings.BaseUri.Host.Contains("zerossl.com");

                // Warn about unneeded EAB
                if (!eabFlow && !string.IsNullOrWhiteSpace(kid))
                {
                    eabFlow = true;
                    _input.CreateSpace();
                    _input.Show(null, "You have provided an external account binding key, even though " +
                        "the server does not indicate that this is required. We will attempt to register " +
                        "using this key anyway.");
                }

                if (zeroSslFlow)
                {
                    async Task emailRegistration()
                    {
                        var registration = await GetContacts(allowMultiple: false, prefix: "");
                        var eab = await _zeroSsl.Register(registration.FirstOrDefault() ?? "");
                        if (eab != null)
                        {
                            kid = eab.Kid;
                            key = eab.Hmac;
                        }
                        else
                        {
                            _log.Error("Unable to retrieve EAB credentials using the provided email address");
                        }
                    }
                    async Task apiKeyRegistration()
                    {
                        var accessKey = await _input.ReadPassword("API access key");
                        var eab = await _zeroSsl.Obtain(accessKey ?? "");
                        if (eab != null)
                        {
                            kid = eab.Kid;
                            key = eab.Hmac;
                        }
                        else
                        {
                            _log.Error("Unable to retrieve EAB credentials using the provided API access key");
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(_accountArguments.EmailAddress))
                    {
                        await emailRegistration();
                    } 
                    else
                    {
                        var instruction = "ZeroSsl can be used either by setting up a new " +
                            "account using your email address or by connecting it to your existing " +
                            "account using the API access key or pre-generated EAB credentials, which can " +
                            "be obtained from the Developer section of the dashboard.";
                        _input.CreateSpace();
                        _input.Show(null, instruction);
                        var chosen = await _input.ChooseFromMenu(
                            "How would you like to create the account?",
                            new List<Choice<Func<Task>>>()
                            {
                                Choice.Create((Func<Task>)apiKeyRegistration, "API access key"),
                                Choice.Create((Func<Task>)emailRegistration, "Email address"),
                                Choice.Create<Func<Task>>(() => Task.CompletedTask, "Input EAB credentials directly")
                            });
                        await chosen.Invoke();
                    }
                }

                if (eabFlow)
                {
                    if (string.IsNullOrWhiteSpace(kid))
                    {
                        var instruction = "This ACME endpoint requires an external account. " +
                            "You will need to provide a key identifier and a key to proceed. " +
                            "Please refer to the providers instructions on how to obtain these.";
                        _input.CreateSpace();
                        _input.Show(null, instruction);
                        kid = await _input.RequestString("Key identifier");
                    }
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        key = await _input.ReadPassword("Key (base64url encoded)");
                    }
                    externalAccount = new ExternalAccountBinding(
                            alg,
                            JsonConvert.SerializeObject(
                                client.Signer.ExportJwk(), 
                                Formatting.None), 
                            kid, 
                            key ?? "", 
                            client.Directory?.NewAccount ?? "");
                } 
                else
                {
                    contacts = await GetContacts();
                }

                try
                {
                    account = await client.CreateAccountAsync(
                        contacts, 
                        termsOfServiceAgreed: true, 
                        externalAccountBinding: externalAccount?.Payload() ?? null);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error creating account");
                }

                if (account != null)
                {
                    try
                    {
                        _log.Debug("Saving account");
                        var accountKey = new AccountSigner
                        {
                            KeyType = client.Signer.JwsAlg,
                            KeyExport = client.Signer.Export(),
                        };
                        AccountSigner = accountKey;
                        await File.WriteAllTextAsync(AccountPath, JsonConvert.SerializeObject(account));
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error saving account");
                        account = null;
                    }
                }

            }
            return account;
        }

        /// <summary>
        /// Ask the user to accept the terms of service dictated 
        /// by the ACME service operator
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task<bool> AcceptTos(string filename, byte[] content)
        {
            var tosPath = Path.Combine(_settings.Client.ConfigurationPath, filename);
            _log.Verbose("Writing terms of service to {path}", tosPath);
            await File.WriteAllBytesAsync(tosPath, content);
            _input.Show($"Terms of service", tosPath);
            if (_arguments.GetArguments<AccountArguments>()?.AcceptTos ?? false)
            {
                return true;
            }
            if (await _input.PromptYesNo($"Open in default application?", false))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tosPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to start application");
                }
            }
            return await _input.PromptYesNo($"Do you agree with the terms?", true);
        }

        /// <summary>
        /// Test the network connection
        /// </summary>
        internal async Task CheckNetwork()
        {
            using var httpClient = _proxyService.GetHttpClient();
            httpClient.BaseAddress = _settings.BaseUri;
            try
            {
                _log.Verbose("SecurityProtocol setting: {setting}", System.Net.ServicePointManager.SecurityProtocol);
                _ = await httpClient.GetAsync("directory");
            }
            catch (Exception)
            {
                _log.Warning("No luck yet, attempting to force TLS 1.2...");
                _proxyService.SslProtocols = SslProtocols.Tls12;
                using var altClient = _proxyService.GetHttpClient();
                altClient.BaseAddress = _settings.BaseUri;
                try
                {
                    _ = await altClient.GetAsync("directory");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to connect to ACME server");
                    return;
                }
            }
            _log.Debug("Connection OK!");
        }

        /// <summary>
        /// Get contact information
        /// </summary>
        /// <returns></returns>
        private async Task<string[]> GetContacts(bool allowMultiple = true, string prefix = "mailto:")
        {
            var email = _accountArguments.EmailAddress;
            if (string.IsNullOrWhiteSpace(email))
            {
                var question = allowMultiple ?
                    "Enter email(s) for notifications about problems and abuse (comma-separated)" :
                    "Enter email for notifications about problems and abuse";
                email = await _input.RequestString(question);
            }
            var newEmails = new List<string>();
            if (allowMultiple)
            {
                newEmails = email.ParseCsv();
                if (newEmails == null)
                {
                    return new string[] { };
                }
            } 
            else
            {
                newEmails.Add(email);
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
            if (!newEmails.Any())
            {
                _log.Warning("No (valid) email address specified");
            }
            return newEmails.Select(x => $"{prefix}{x}").ToArray();
        }

        /// <summary>
        /// File that contains information about the signer, which
        /// cryptographically signs the messages sent to the ACME 
        /// server so that the account can be authenticated
        /// </summary>
        private string SignerPath => Path.Combine(_settings.Client.ConfigurationPath, SignerFileName);

        /// <summary>
        /// File that contains information about the account
        /// </summary>
        private string AccountPath => Path.Combine(_settings.Client.ConfigurationPath, RegistrationFileName);

        private AccountSigner? AccountSigner
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
                File.WriteAllText(SignerPath, x.DiskValue(_settings.Security.EncryptConfig));
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

        internal async Task<IChallengeValidationDetails> DecodeChallengeValidation(Authorization auth, Challenge challenge)
        {
            var client = await GetClient();
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, client.Signer);
        }

        internal async Task<Challenge> AnswerChallenge(Challenge challenge)
        {
            // Have to loop to wait for server to stop being pending
            var client = await GetClient();
            challenge = await Retry(() => client.AnswerChallengeAsync(challenge.Url));
            var tries = 1;
            while (
                challenge.Status == AuthorizationPending ||
                challenge.Status == AuthorizationProcessing)
            {
                await Task.Delay(_settings.Acme.RetryInterval * 1000);
                _log.Debug("Refreshing authorization ({tries}/{count})", tries, _settings.Acme.RetryCount);
                challenge = await Retry(() => client.GetChallengeDetailsAsync(challenge.Url));
                tries += 1;
                if (tries > _settings.Acme.RetryCount)
                {
                    break;
                }
            }
            return challenge;
        }

        internal async Task<OrderDetails> CreateOrder(IEnumerable<string> identifiers)
        {
            var client = await GetClient();
            return await Retry(() => client.CreateOrderAsync(identifiers));
        }

        internal async Task<Challenge> GetChallengeDetails(string url)
        {
            var client = await GetClient();
            return await Retry(() => client.GetChallengeDetailsAsync(url));
        }

        internal async Task<Authorization> GetAuthorizationDetails(string url)
        {
            var client = await GetClient();
            return await Retry(() => client.GetAuthorizationDetailsAsync(url));
        }

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
        /// </summary>
        /// <param name="details"></param>
        /// <param name="csr"></param>
        /// <returns></returns>
        internal async Task<OrderDetails> SubmitCsr(OrderDetails details, byte[] csr)
        {
            // First wait for the order to get "ready", meaning that all validations
            // are complete. The program makes sure this is the case at the level of 
            // individual authorizations, but the server might need some extra time to
            // propagate this status at the order level.
            var client = await GetClient();
            await WaitForOrderStatus(details, OrderReady);
            if (details.Payload.Status == OrderReady)
            {
                details = await Retry(() => client.FinalizeOrderAsync(details.Payload.Finalize, csr));
                await WaitForOrderStatus(details, OrderProcessing, true);
            } 
            return details;
        }

        /// <summary>
        /// Helper function to check/refresh order state
        /// </summary>
        /// <param name="details"></param>
        /// <param name="status"></param>
        /// <param name="negate"></param>
        /// <returns></returns>
        private async Task WaitForOrderStatus(OrderDetails details, string status, bool negate = false)
        {
            // Wait for processing
            _ = await GetClient();
            var tries = 0;
            do
            {
                if (tries > 0)
                {
                    if (tries > _settings.Acme.RetryCount)
                    {
                        break;
                    }
                    _log.Debug($"Waiting for order to get {(negate ? "NOT " : "")}{{ready}} ({{tries}}/{{count}})", status, tries, _settings.Acme.RetryCount);
                    await Task.Delay(_settings.Acme.RetryInterval * 1000);
                    var update = await GetOrderDetails(details.OrderUrl);
                    details.Payload = update.Payload;
                }
                tries += 1;
            } while (
                (negate && details.Payload.Status == status) ||
                (!negate && details.Payload.Status != status)
            );
        }

        internal async Task<OrderDetails> GetOrderDetails(string url)
        {
            var client = await GetClient();
            return await Retry(() => client.GetOrderDetailsAsync(url));
        }

        internal async Task ChangeContacts()
        {
            var client = await GetClient();
            var contacts = await GetContacts();
            var account = await Retry(() => client.UpdateAccountAsync(contacts, client.Account));
            await UpdateAccount();
        }

        internal async Task UpdateAccount()
        {
            var client = await GetClient();
            var account = await Retry(() => client.CheckAccountAsync());
            File.WriteAllText(AccountPath, JsonConvert.SerializeObject(account));
            client.Account = account;
        }

        internal async Task<AcmeCertificate> GetCertificate(OrderDetails order)
        {
            var client = await GetClient();
            return await Retry(() => client.GetOrderCertificateExAsync(order));
        }

        internal async Task RevokeCertificate(byte[] crt)
        {
            var client = await GetClient();
            _ = await Retry(async () => client.RevokeCertificateAsync(crt, RevokeReason.Unspecified));
        }

        /// <summary>
        /// According to the ACME standard, we SHOULD retry calls
        /// if there is an invalid nonce. TODO: check for the proper 
        /// exception feedback, now *any* failed request is retried
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        private async Task<T> Retry<T>(Func<Task<T>> executor, int attempt = 0)
        {
            if (attempt == 0)
            {
                await _requestLock.WaitAsync();  
            }
            try
            {
                return await executor();
            }
            catch (AcmeProtocolException apex)
            {
                if (attempt < 3 && apex.ProblemType == ProblemType.BadNonce)
                {
                    _log.Warning("First chance error calling into ACME server, retrying with new nonce...");
                    var client = await GetClient();
                    await client.GetNonceAsync();
                    return await Retry(executor, attempt + 1);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (attempt == 0)
                {
                    _requestLock.Release();
                }
            }
        }

        /// <summary>
        /// Prevent sending simulateous requests to the ACME service because it messes
        /// up the nonce tracking mechanism
        /// </summary>
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && _client != null)
                {
                    _client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AcmeClient()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);// TODO: uncomment the following line if the finalizer is overridden above.// GC.SuppressFinalize(this);
        #endregion
    }
}
