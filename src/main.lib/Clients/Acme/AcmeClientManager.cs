using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
    internal class AcmeClientManager
    {
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly ISettingsService _settings;
        private readonly ArgumentsParser _arguments;
        private readonly IProxyService _proxyService;
        private readonly ZeroSsl _zeroSsl;
        private readonly AccountArguments _accountArguments;

        private AcmeProtocolClient? _anonymousClient;
        private AcmeClient? _authorizedClient;
        private readonly AccountManager _accountManager;
        private Account? _account;
        private bool _initialized = false;

        public AcmeClientManager(
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
            _anonymousClient = client;

            // Try to load prexisting account
            var account = _accountManager.DefaultAccount; 
            if (account != null)
            {
                _log.Verbose("Using existing default ACME account");
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
            }

            // Switch to the created/selected account
            UseAccount(account);
        }

        /// <summary>
        /// Start using the account
        /// </summary>
        /// <param name="account"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void UseAccount(Account account)
        {
            if (_anonymousClient == null)
            {
                throw new InvalidOperationException();
            }
            _account = account;
            _authorizedClient = new AcmeClient(_log, _settings, _proxyService, _anonymousClient.Directory, account);
            _log.Verbose("Using account {account}...", account.Details.Payload.Id);
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
                directory = await client.Backoff(async () => await client.GetDirectoryAsync("directory"), _log);
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
            directory = await client.Backoff(async () => await client.GetDirectoryAsync(""), _log);
            if (directory != null)
            {
                return directory;
            }
            throw new Exception("Unable to get service directory");
        }

        /// <summary>
        /// Get AcmeClient using the default account
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal async Task<AcmeClient> GetClient()
        {
            if (!_initialized)
            {
                await ConfigureAcmeClient();
                _initialized = true;
            }
            if (_authorizedClient == null)
            {
                throw new InvalidOperationException("Failed to initialize Acme client");
            }
            return _authorizedClient;
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
            return await client.Retry(
                () => client.CreateAccountAsync(
                    contacts,
                    termsOfServiceAgreed: true,
                    externalAccountBinding: externalAccount?.Payload() ?? null), _log);
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

        /// <summary>
        /// Update email address for the current account
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal async Task ChangeContacts()
        {
            if (_account == null)
            {
                throw new InvalidOperationException();
            }
            var client = await GetClient();
            var contacts = await GetContacts();
            var newDetails = await client.UpdateAccountAsync(contacts);
            if (newDetails != null)
            {
                _account.Details = newDetails;
                _accountManager.DefaultAccount = _account;
            }
        }
    }
}