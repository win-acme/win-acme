using ACMESharp;
using ACMESharp.Authorizations;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Protocol = ACMESharp.Protocol.Resources;

namespace PKISharp.WACS.Clients.Acme
{
    [JsonSerializable(typeof(AccountSigner))]
    [JsonSerializable(typeof(AccountDetails))]
    [JsonSerializable(typeof(ServiceDirectory))]
    [JsonSerializable(typeof(AcmeOrderDetails))]
    internal partial class AcmeClientJson : JsonSerializerContext
    {
        public static AcmeClientJson Insensitive { get; } = new AcmeClientJson(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// Main class that talks to the ACME server
    /// </summary>
    internal class AcmeClient
    {
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
        private readonly ArgumentsParser _arguments;
        private readonly IProxyService _proxyService;
        private readonly ZeroSsl _zeroSsl;
        private readonly AccountArguments _accountArguments;

        private AcmeProtocolClient? _client;
        private readonly AccountManager _accountManager;
        private Account? _account;
        private bool _initialized = false;

        public AcmeClient(
            IInputService inputService,
            ArgumentsParser arguments,
            ILogService log,
            ISettingsService settings,
            AccountManager accountManager,
            IProxyService proxy,
            ZeroSsl zeroSsl)
        {
            _log = log;
            _settings = settings;
            _arguments = arguments;
            _accountArguments = _arguments.GetArguments<AccountArguments>() ?? new AccountArguments();
            _input = inputService;
            _proxyService = proxy;
            _accountManager = accountManager;
            _account = accountManager.DefaultAccount;
            _zeroSsl = zeroSsl;
        }

        /// <summary>
        /// Load the account, signer and directory
        /// </summary>
        /// <returns></returns>
        internal async Task ConfigureAcmeClient()
        {
            var httpClient = _proxyService.GetHttpClient();
            httpClient.BaseAddress = _settings.BaseUri;
            _log.Verbose("Constructing ACME protocol client...");
            var client = new AcmeProtocolClient(httpClient, usePostAsGet: _settings.Acme.PostAsGet);
            client.Directory = await EnsureServiceDirectory(client);

            // Try to load prexisting account
            var account = _accountManager.DefaultAccount;
            if (account != null)
            {
                _log.Verbose("Using existing default ACME account");
                await UseAccount(client, account);
            }
            else
            {
                _log.Verbose("No account found, creating new one");
                account = await SetupAccount(client);
                if (account == null)
                {
                    throw new Exception("AcmeClient was unable to find or create an account");
                }
                // Save newly created account to disk
                _accountManager.DefaultAccount = account;
                // Start using it
                await UseAccount(client, account);
            }

            _client = client;
            _log.Verbose("ACME client initialized");
        }

        /// <summary>
        /// Set the account to use
        /// </summary>
        /// <returns></returns>
        internal async Task UseAccount(AcmeProtocolClient client, Account account)
        {
            _log.Verbose("Using ACME account {id}", account.Details.Kid);
            client.Account = null;
            await client.ChangeAccountKeyAsync(account.Signer.JwsTool());
            client.Account = account.Details;
            client.NextNonce = null;
            _account = account;
        }

        /// <summary>
        /// Make sure that we find a service directory
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        internal async Task<ServiceDirectory> EnsureServiceDirectory(AcmeProtocolClient client)
        {
            ServiceDirectory? directory;
            try
            {
                _log.Verbose("Getting service directory...");
                directory = await Backoff(async () => await client.GetDirectoryAsync("directory"));
                if (directory != null)
                {
                    return directory;
                }
            }
            catch
            {

            }
            // Perhaps the BaseUri *is* the directory, such
            // as implemented by Digicert (#1434)
            directory = await Backoff(async () => await client.GetDirectoryAsync(""));
            if (directory != null)
            {
                return directory;
            }
            throw new Exception("Unable to get service directory");
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

        /// <summary>
        /// Setup a new ACME account
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task<Account?> SetupAccount(AcmeProtocolClient client)
        {
            // Accept the terms of service, if defined by the server
            try
            {
                var (_, filename, content) = await client.GetTermsOfServiceAsync();
                _log.Verbose("Terms of service downloaded");
                if (!string.IsNullOrEmpty(filename) && content != null)
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

            var eabKid = _accountArguments.EabKeyIdentifier;
            var eabKey = _accountArguments.EabKey;
            var eabAlg = _accountArguments.EabAlgorithm ?? "HS256";
            var eabFlow = client.Directory?.Meta?.ExternalAccountRequired ?? false;
            var zeroSslFlow = _settings.BaseUri.Host.Contains("zerossl.com");

            // Warn about unneeded EAB
            if (!eabFlow && !string.IsNullOrWhiteSpace(eabKid))
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
                        eabKid = eab.Kid;
                        eabKey = eab.Hmac;
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
                        eabKid = eab.Kid;
                        eabKey = eab.Hmac;
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
                else if (!eabFlow)
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
                           Choice.Create(apiKeyRegistration, "API access key"),
                           Choice.Create(emailRegistration, "Email address"),
                           Choice.Create<Func<Task>>(() => Task.CompletedTask, "Input EAB credentials directly")
                        });
                    await chosen.Invoke();
                }
            }

            if (eabFlow)
            {
                if (string.IsNullOrWhiteSpace(eabKid))
                {
                    var instruction = "This ACME endpoint requires an external account. " +
                        "You will need to provide a key identifier and a key to proceed. " +
                        "Please refer to the providers instructions on how to obtain these.";
                    _input.CreateSpace();
                    _input.Show(null, instruction);
                    eabKid = await _input.RequestString("Key identifier");
                }
                if (string.IsNullOrWhiteSpace(eabKey))
                {
                    eabKey = await _input.ReadPassword("Key (base64url encoded)");
                }
                contacts = await GetContacts(runLevel: RunLevel.Unattended);
            }
            else
            {
                contacts = await GetContacts();
            }

            var newAccount = _accountManager.NewAccount();
            var newAccountDetails = default(AccountDetails);
            try
            {
                newAccountDetails = await CreateAccount(client, newAccount.Signer, contacts, eabAlg, eabKid, eabKey);
            }
            catch (AcmeProtocolException apex)
            {
                // Some non-ACME compliant server may not support ES256 or other
                // algorithms, so attempt fallback to RS256
                if (apex.ProblemType == ProblemType.BadSignatureAlgorithm && newAccount.Signer.KeyType != "RS256")
                {
                    newAccount = _accountManager.NewAccount("RS256");
                    newAccountDetails = await CreateAccount(client, newAccount.Signer, contacts, eabAlg, eabKid, eabKey);
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, ex.Message);
                return null;
            }
            if (newAccountDetails == default)
            {
                return null;
            }
            return new Account(newAccountDetails, newAccount.Signer);
        }

        /// <summary>
        /// Attempt to create an account using specific parameters
        /// </summary>
        /// <param name="client"></param>
        /// <param name="signer"></param>
        /// <param name="contacts"></param>
        /// <param name="eabAlg"></param>
        /// <param name="eabKid"></param>
        /// <param name="eabKey"></param>
        /// <returns></returns>
        private async Task<AccountDetails> CreateAccount(
            AcmeProtocolClient client, AccountSigner signer,
            string[]? contacts,
            string eabAlg, string? eabKid, string? eabKey)
        {
            if (client.Account != null)
            {
                throw new Exception("Client already has an account!");
            }
            ExternalAccountBinding? externalAccount = null;
            if (!string.IsNullOrWhiteSpace(eabKey) &&
                !string.IsNullOrWhiteSpace(eabKid))
            {
                externalAccount = new ExternalAccountBinding(
                    eabAlg,
                    signer.JwsTool().ExportEab(),
                    eabKid,
                    eabKey,
                    client.Directory?.NewAccount ?? "");
            }
            await client.ChangeAccountKeyAsync(signer.JwsTool());
            return await Retry(client,
                () => client.CreateAccountAsync(
                    contacts,
                    termsOfServiceAgreed: true,
                    externalAccountBinding: externalAccount?.Payload() ?? null));
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
            _input.CreateSpace();
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
        /// Get contact information
        /// </summary>
        /// <returns></returns>
        private async Task<string[]> GetContacts(
            bool allowMultiple = true,
            string prefix = "mailto:",
            RunLevel runLevel = RunLevel.Interactive)
        {
            var email = _accountArguments.EmailAddress;
            if (string.IsNullOrWhiteSpace(email) && runLevel.HasFlag(RunLevel.Interactive))
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
                    return Array.Empty<string>();
                }
            }
            else if (!string.IsNullOrWhiteSpace(email))
            {
                newEmails.Add(email);
            }
            newEmails = newEmails.Where(x =>
            {
                try
                {
                    _ = new MailAddress(x);
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

        internal async Task<IChallengeValidationDetails> DecodeChallengeValidation(AcmeAuthorization auth, AcmeChallenge challenge)
        {
            var client = await GetClient();
            if (challenge.Type == null)
            {
                throw new NotSupportedException("Missing challenge type");
            }
            return AuthorizationDecoder.DecodeChallengeValidation(auth, challenge.Type, client.Signer);
        }

        internal async Task<AcmeChallenge> AnswerChallenge(AcmeChallenge challenge)
        {
            // Have to loop to wait for server to stop being pending
            var client = await GetClient();
            if (challenge.Url == null)
            {
                throw new NotSupportedException("Missing challenge url");
            }
            challenge = await Retry(client, () => client.AnswerChallengeAsync(challenge.Url));
            var tries = 1;
            while (
                challenge.Status == AuthorizationPending ||
                challenge.Status == AuthorizationProcessing)
            {
                if (challenge.Url == null)
                {
                    throw new NotSupportedException("Missing challenge url");
                }
                await Task.Delay(_settings.Acme.RetryInterval * 1000);
                _log.Debug("Refreshing authorization ({tries}/{count})", tries, _settings.Acme.RetryCount);
                challenge = await Retry(client, () => client.GetChallengeDetailsAsync(challenge.Url));
                tries += 1;
                if (tries > _settings.Acme.RetryCount)
                {
                    break;
                }
            }
            return challenge;
        }

        internal async Task<AcmeOrderDetails> CreateOrder(IEnumerable<Identifier> identifiers)
        {
            var client = await GetClient();
            var acmeIdentifiers = identifiers.Select(i => new AcmeIdentifier() {
                Type = i.Type switch
                {
                    IdentifierType.DnsName => "dns", // rfc8555
                    IdentifierType.IpAddress => "ip", // rfc8738
                    _ => throw new NotImplementedException($"Identifier {i.Type} is not supported")
                },
                Value = i.Value
            });
            return await Retry(client, () => client.CreateOrderAsync(acmeIdentifiers));
        }

        internal async Task<AcmeChallenge> GetChallengeDetails(string url)
        {
            var client = await GetClient();
            return await Retry(client, () => client.GetChallengeDetailsAsync(url));
        }

        internal async Task<AcmeAuthorization> GetAuthorizationDetails(string url)
        {
            var client = await GetClient();
            return await Retry(client, () => client.GetAuthorizationDetailsAsync(url));
        }

        internal async Task DeactivateAuthorization(string url)
        {
            var client = await GetClient();
            await Retry(client, () => client.DeactivateAuthorizationAsync(url));
        }

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
        /// </summary>
        /// <param name="details"></param>
        /// <param name="csr"></param>
        /// <returns></returns>
        internal async Task<AcmeOrderDetails> SubmitCsr(AcmeOrderDetails details, byte[] csr)
        {
            // First wait for the order to get "ready", meaning that all validations
            // are complete. The program makes sure this is the case at the level of 
            // individual authorizations, but the server might need some extra time to
            // propagate this status at the order level.
            var client = await GetClient();
            await WaitForOrderStatus(details, OrderReady);
            if (details.Payload.Status == OrderReady)
            {
                if (string.IsNullOrEmpty(details.Payload.Finalize))
                {
                    throw new Exception("Missing Finalize url");
                }
                details = await Retry(client, () => client.FinalizeOrderAsync(details, csr));
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
        private async Task WaitForOrderStatus(AcmeOrderDetails details, string status, bool negate = false)
        {
            if (details.OrderUrl == null)
            {
                throw new InvalidOperationException();
            }

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

        internal async Task<AcmeOrderDetails> GetOrderDetails(string url)
        {
            var client = await GetClient();
            return await Retry(client, () => client.GetOrderDetailsAsync(url));
        }

        internal async Task ChangeContacts()
        {
            var client = await GetClient();
            var contacts = await GetContacts();
            var account = await Retry(client, () => client.UpdateAccountAsync(contacts));
            await UpdateAccount(client);
        }

        internal async Task UpdateAccount(AcmeProtocolClient client)
        {
            if (_account == null)
            {
                throw new InvalidOperationException();
            }
            var newDetails = await Retry(client, client.CheckAccountAsync);
            if (newDetails != null)
            {
                _account.Details = newDetails;
                _accountManager.DefaultAccount = _account;
            }
            await UseAccount(client, _account);
        }

        internal async Task<AcmeCertificate> GetCertificate(AcmeOrderDetails order)
        {
            var client = await GetClient();
            return await Retry(client, () => client.GetOrderCertificateExAsync(order));
        }

        internal async Task<byte[]> GetCertificate(string url)
        {
            var client = await GetClient();
            return await Retry(client, async () => {
                var response = await client.GetAsync(url);
                return await response.Content.ReadAsByteArrayAsync();
            });
        }

        internal async Task<bool> RevokeCertificate(byte[] crt)
        {
            var client = await GetClient();
            return await Retry(client, () => client.RevokeCertificateAsync(crt, Protocol.RevokeReason.Unspecified));
        }

        /// <summary>
        /// According to the ACME standard, we SHOULD retry calls
        /// if there is an invalid nonce. TODO: check for the proper 
        /// exception feedback, now *any* failed request is retried
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <returns></returns>
        private async Task<T> Retry<T>(AcmeProtocolClient client, Func<Task<T>> executor, int attempt = 0)
        {
            if (attempt == 0)
            {
                await _requestLock.WaitAsync();
            }
            try
            {
                return await Backoff(async () => {
                    if (string.IsNullOrEmpty(client.NextNonce))
                    {
                        await GetNonce(client);
                    }
                    return await executor();
                });
            }
            catch (AcmeProtocolException apex)
            {
                if (attempt < 3 && apex.ProblemType == Protocol.ProblemType.BadNonce)
                {
                    _log.Warning("First chance error calling into ACME server, retrying with new nonce...");
                    await GetNonce(client);
                    return await Retry(client, executor, attempt + 1);
                }
                else if (apex.ProblemType == Protocol.ProblemType.UserActionRequired)
                {
                    _log.Error("{detail}: {instance}", apex.ProblemDetail, apex.ProblemInstance);
                    throw;
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
        /// Get a new nonce to use by the client
        /// </summary>
        /// <returns></returns>
        internal async Task GetNonce(AcmeProtocolClient client) => await Backoff(async () => {
            await client.GetNonceAsync();
            return 1;
        });

        /// <summary>
        /// Retry a call to the AcmeService up to five times, with a bigger
        /// delay for each time that the call fails with a TooManyRequests 
        /// response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executor"></param>
        /// <param name="attempt"></param>
        /// <returns></returns>
        internal async Task<T> Backoff<T>(Func<Task<T>> executor, int attempt = 0)
        {
            try
            {
                return await executor();
            }
            catch (AcmeProtocolException ape)
            {
                if (ape.Response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (ape.ProblemType == Protocol.ProblemType.RateLimited)
                    {
                        // Do not keep retrying when rate limit is hit
                        throw;
                    }
                    if (attempt == 5)
                    {
                        throw new Exception("Service is too busy, try again later...", ape);
                    }
                    var delaySeconds = (int)Math.Pow(2, attempt + 3); // 5 retries with 8 to 128 seconds delay
                    _log.Warning("Service is busy at the moment, backing off for {n} seconds", delaySeconds);
                    await Task.Delay(1000 * delaySeconds);
                    return await Backoff(executor, attempt + 1);
                }
                throw;
            }
        }

        /// <summary>
        /// Prevent sending simulateous requests to the ACME service because it messes
        /// up the nonce tracking mechanism
        /// </summary>
        private readonly SemaphoreSlim _requestLock = new(1, 1);
    }
}
